using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

namespace Deveroom.SampleProjectGenerator
{
    public abstract class ProjectGenerator : IProjectGenerator
    {
        protected readonly GeneratorOptions _options;
        protected readonly Action<string> _consoleWriteLine;

        public string TargetFolder => _options.TargetFolder;
        public string TargetFramework => _options.TargetFramework;
        public virtual string PackagesFolder => "packages";
        public string AssemblyName => "DeveroomSample";
        public string AssemblyFileName => AssemblyName + ".dll";
        public abstract string GetOutputAssemblyPath(string config = "Debug");
        public List<string> FeatureFiles { get; } = new List<string>();
        public List<NuGetPackageData> InstalledNuGetPackages { get; } = new List<NuGetPackageData>();

        protected ProjectGenerator(GeneratorOptions options, Action<string> consoleWriteLine = null)
        {
            _options = options;
            _consoleWriteLine = consoleWriteLine ?? Console.WriteLine;
        }

        public void Generate()
        {
            _consoleWriteLine(_options.TargetFolder);
            if (!_options.Force && 
                Directory.Exists(Path.Combine(_options.TargetFolder, ".git")) && 
                File.Exists(Path.Combine(_options.TargetFolder, "DeveroomSample.csproj")))
            {
                if (GitReset())
                {
                    ScanExistingProjectFolder();
                    return;
                }
            }

            EnsureEmptyFolder(_options.TargetFolder);

            var templatesFolder = GetTemplatesFolder();
            string projectFilePath = null;
            foreach (var template in Directory.GetFiles(templatesFolder, "*.txt"))
            {
                var destFileName = Path.Combine(_options.TargetFolder, Path.GetFileNameWithoutExtension(template));
                File.Copy(template, destFileName, true);
                if (destFileName.EndsWith("proj"))
                    projectFilePath = destFileName;
            }

            var packagesFolder = GetPackagesFolder();

            if (_options.PlatformTarget != null)
                SetPlatformTarget(projectFilePath);

            if (_options.TargetFramework != GeneratorOptions.DefaultTargetFramework)
                SetTargetFramework(projectFilePath);

            switch (_options.UnitTestProvider.ToLowerInvariant())
            {
                case "nunit":
                    InstallNUnit(packagesFolder, projectFilePath);
                    break;
                case "xunit":
                    InstallXUnit(packagesFolder, projectFilePath);
                    break;
                case "mstest":
                    InstallMsTest(packagesFolder, projectFilePath);
                    break;
                default:
                    throw new NotSupportedException(_options.UnitTestProvider);
            }

            InstallSpecFlow(packagesFolder, projectFilePath);

            GenerateTestArtifacts(projectFilePath);

            if (_options.AddGeneratorPlugin || _options.AddRuntimePlugin)
            {
                InstallSpecFlowPlugin(packagesFolder, projectFilePath);
            }

            if (_options.AddExternalBindingPackage)
                InstallExternalBindingPackage(packagesFolder, projectFilePath);

            if (_options.IsBuilt)
            {
                BuildProject();

                File.WriteAllText(Path.Combine(_options.TargetFolder, ".gitignore"), "");
            }

            GitInit();
        }

        protected virtual void SetTargetFramework(string projectFilePath)
        {
            var projectChanger = CreateProjectChanger(projectFilePath);
            projectChanger.SetTargetFramework(_options.TargetFramework);
            projectChanger.Save();
        }

        protected virtual void ScanExistingProjectFolder()
        {
            FeatureFiles.AddRange(Directory.GetFiles(_options.TargetFolder, "*.feature", SearchOption.AllDirectories));
            var projectFilePath = Directory.GetFiles(TargetFolder, "*.csproj").FirstOrDefault();
            if (projectFilePath == null)
                throw new Exception("Unable to detect project file");
            var projectChanger = CreateProjectChanger(projectFilePath);
            InstalledNuGetPackages.AddRange(projectChanger.GetInstalledNuGetPackages(GetPackagesFolder()));
        }

        protected abstract string GetTemplatesFolder();
        protected abstract string GetPackagesFolder();
        protected abstract ProjectChanger CreateProjectChanger(string projectFilePath);

        protected virtual void BuildProject()
        {
            var exitCode = ExecMsBuild();
            if (exitCode != 0)
            {
                _consoleWriteLine($"Build exit code: {exitCode}");
                throw new Exception($"Build failed with exit code {exitCode}");
            }
        }

        private void SetPlatformTarget(string projectFilePath)
        {
            var projectChanger = CreateProjectChanger(projectFilePath);
            projectChanger.SetPlatformTarget(_options.PlatformTarget);
            projectChanger.Save();
        }

        private void GenerateTestArtifacts(string projectFilePath)
        {
            var customTool = _options.SpecFlowVersion >= new Version("3.0") ? null : "SpecFlowSingleFileGenerator";

                var featuresFolder = Path.Combine(_options.TargetFolder, "Features");
            var stepDefsFolder = Path.Combine(_options.TargetFolder, "StepDefinitions");
            EnsureEmptyFolder(featuresFolder);
            EnsureEmptyFolder(stepDefsFolder);

            var stepCount = _options.FeatureFileCount * _options.ScenarioPerFeatureFileCount * 4;
            var assetGenerator = new SpecFlowAssetGenerator(Math.Max(3, stepCount * _options.StepDefinitionPerStepPercent / 100));
            if (_options.AddUnicodeBinding)
                assetGenerator.AddUnicodeSteps();
            if (_options.AddAsyncStep)
                assetGenerator.AddAsyncStep();
            var projectChanger = CreateProjectChanger(projectFilePath);
            for (int i = 0; i < _options.FeatureFileCount; i++)
            {
                var scenarioOutlineCount =
                    _options.ScenarioPerFeatureFileCount * _options.ScenarioOutlinePerScenarioPercent / 100;
                var scenarioCount = _options.ScenarioPerFeatureFileCount - scenarioOutlineCount;
                var filePath = assetGenerator.GenerateFeatureFile(featuresFolder, scenarioCount, scenarioOutlineCount);
                projectChanger.AddFile(filePath, "None", customTool);
                FeatureFiles.Add(filePath);
            }
            var stepDefClasses = assetGenerator.GenerateStepDefClasses(stepDefsFolder, _options.StepDefPerClassCount);
            foreach (var stepDefClass in stepDefClasses)
            {
                projectChanger.AddFile(stepDefClass, "Compile");
            }
            projectChanger.Save();

            _consoleWriteLine(
                $"Generated {assetGenerator.StepDefCount} step definitions, {_options.FeatureFileCount * _options.ScenarioPerFeatureFileCount} scenarios, {assetGenerator.StepCount} steps, {stepDefClasses.Count} step definition classes, {_options.FeatureFileCount} feature files");
        }

        private void ExecNuGetInstall(string packageName, string packagesFolder, params string[] otherArgs)
        {
            var args = new[]
            {
                "install", packageName, "-OutputDirectory", packagesFolder,
                "-Source", "https://api.nuget.org/v3/index.json"
            }.AsEnumerable();
            if (otherArgs != null)
                args = args.Concat(otherArgs);
            if (_options.FallbackNuGetPackageSource != null)
                args = args.Concat(new []{ "-FallbackSource", _options.FallbackNuGetPackageSource });
            ExecNuGet(args.ToArray());
        }

        private void InstallExternalBindingPackage(string packagesFolder, string projectFilePath)
        {
            ExecNuGetInstall(_options.ExternalBindingPackageName, packagesFolder);
            var projectChanger = CreateProjectChanger(projectFilePath);
            InstallNuGetPackage(projectChanger, packagesFolder, _options.ExternalBindingPackageName);
            projectChanger.SetSpecFlowConfig("stepAssemblies/stepAssembly", "assembly", _options.ExternalBindingPackageName);
            projectChanger.Save();
        }

        private void InstallSpecFlowPlugin(string packagesFolder, string projectFilePath)
        {
            ExecNuGetInstall(_options.PluginName, packagesFolder);
            var projectChanger = CreateProjectChanger(projectFilePath);
            InstallNuGetPackage(projectChanger, packagesFolder, _options.PluginName);
            projectChanger.SetSpecFlowConfig("plugins/add", "name", _options.PluginName.Replace(".SpecFlowPlugin", ""));
            projectChanger.SetSpecFlowConfig("plugins/add", "type", _options.AddRuntimePlugin && _options.AddGeneratorPlugin ? "GeneratorAndRuntime" : _options.AddGeneratorPlugin ? "Generator" : "Runtime");
            projectChanger.Save();
        }

        private bool GitReset()
        {
            var exitCode = ExecGit("reset", "--hard");
            ExecGit("clean", "-fdx", "-e", "packages");
            ExecGit("status");
            if (exitCode != 0)
                _consoleWriteLine($"Git status exit code: {exitCode}");
            return exitCode == 0;
        }

        private void GitInit()
        {
            ExecGit("init");
            GitCommitAll();
        }

        private void GitCommitAll()
        {
            ExecGit("add", ".");
            ExecGit("commit", "-q", "-m", "init");
        }

        private void InstallNUnit(string packagesFolder, string projectFilePath)
        {
            if (_options.SpecFlowVersion >= new Version("2.0"))
                ExecNuGetInstall("NUnit", packagesFolder);
            else //v1.9
                ExecNuGetInstall("NUnit", packagesFolder, "-Version", "3.0.0");
            ExecNuGetInstall("NUnit3TestAdapter", packagesFolder);

            var projectChanger = CreateProjectChanger(projectFilePath);
            InstallNuGetPackage(projectChanger, packagesFolder, "NUnit");
            InstallNuGetPackage(projectChanger, packagesFolder, "NUnit3TestAdapter", "net35");
            projectChanger.Save();
        }

        private void InstallXUnit(string packagesFolder, string projectFilePath)
        {
            ExecNuGetInstall("xUnit", packagesFolder);
            ExecNuGetInstall("xunit.runner.visualstudio", packagesFolder);

            var projectChanger = CreateProjectChanger(projectFilePath);
            InstallNuGetPackage(projectChanger, packagesFolder, "xunit.core");
            InstallNuGetPackage(projectChanger, packagesFolder, "xunit.abstractions", "net35");
            InstallNuGetPackage(projectChanger, packagesFolder, "xunit.assert", "netstandard1.1");
            InstallNuGetPackage(projectChanger, packagesFolder, "xunit.extensibility.core", "netstandard1.1");
            InstallNuGetPackage(projectChanger, packagesFolder, "xunit.extensibility.execution", "net452");
            InstallNuGetPackage(projectChanger, packagesFolder, "xunit.runner.visualstudio", "net20");
            projectChanger.Save();
        }

        private void InstallMsTest(string packagesFolder, string projectFilePath)
        {
            ExecNuGetInstall("MSTest.TestFramework", packagesFolder);
            ExecNuGetInstall("MSTest.TestAdapter", packagesFolder);

            var projectChanger = CreateProjectChanger(projectFilePath);
            InstallNuGetPackage(projectChanger, packagesFolder, "MSTest.TestFramework");
            InstallNuGetPackage(projectChanger, packagesFolder, "MSTest.TestAdapter");
            projectChanger.Save();
        }

        private void InstallSpecFlow(string packagesFolder, string projectFilePath)
        {
            ExecNuGetInstall("SpecFlow", packagesFolder, "-Version", _options.SpecFlowPackageVersion);

            var projectChanger = CreateProjectChanger(projectFilePath);
            InstallSpecFlowPackages(packagesFolder, projectChanger);
            SetSpecFlowUnitTestProvider(projectChanger, packagesFolder);
            projectChanger.Save();
        }

        protected virtual void SetSpecFlowUnitTestProvider(ProjectChanger projectChanger, string packagesFolder)
        {
            if (_options.SpecFlowVersion >= new Version("3.0"))
            {
                var sourcePlatform = GetSpecFlowSourcePlatform();
                ExecNuGetInstall("SpecFlow.Tools.MsBuild.Generation", packagesFolder, "-Version", _options.SpecFlowPackageVersion);
                InstallNuGetPackage(projectChanger, packagesFolder, "SpecFlow.Tools.MsBuild.Generation", sourcePlatform, _options.SpecFlowPackageVersion);

                ExecNuGetInstall("SpecFlow." + _options.UnitTestProvider, packagesFolder, "-Version", _options.SpecFlowPackageVersion);
                InstallNuGetPackage(projectChanger, packagesFolder, "SpecFlow." + _options.UnitTestProvider, sourcePlatform, _options.SpecFlowPackageVersion);
                return;
            }

            projectChanger.SetSpecFlowConfig("unitTestProvider", "name", _options.UnitTestProvider);
        }

        protected virtual void InstallSpecFlowPackages(string packagesFolder, ProjectChanger projectChanger)
        {
            var sourcePlatform = GetSpecFlowSourcePlatform();
            InstallNuGetPackage(projectChanger, packagesFolder, "SpecFlow", sourcePlatform, _options.SpecFlowPackageVersion);

            if (_options.SpecFlowVersion >= new Version("3.1"))
            {
                InstallNuGetPackage(projectChanger, packagesFolder, "Cucumber.Messages", dependency: true, packageVersion: "6.0.1");
                InstallNuGetPackage(projectChanger, packagesFolder, "Google.Protobuf", dependency: true, packageVersion: "3.7.0");
            }

            if (_options.SpecFlowVersion >= new Version("3.7"))
            {
                InstallNuGetPackage(projectChanger, packagesFolder, "BoDi", dependency: true, packageVersion: "1.5.0");
                InstallNuGetPackage(projectChanger, packagesFolder, "Gherkin", dependency: true, packageVersion: "6.0.0");
                InstallNuGetPackage(projectChanger, packagesFolder, "Utf8Json", "net45", dependency: true, packageVersion: "1.3.7");
                InstallNuGetPackage(projectChanger, packagesFolder, "System.ValueTuple", "netstandard1.0", dependency: true);
            }
            else if (_options.SpecFlowVersion >= new Version("3.0.188"))
            {
                InstallNuGetPackage(projectChanger, packagesFolder, "BoDi", dependency: true, packageVersion: "1.4.1");
                InstallNuGetPackage(projectChanger, packagesFolder, "Gherkin", dependency: true, packageVersion: "6.0.0");
                InstallNuGetPackage(projectChanger, packagesFolder, "Utf8Json", "net45", dependency: true, packageVersion: "1.3.7");
                InstallNuGetPackage(projectChanger, packagesFolder, "System.ValueTuple", "netstandard1.0", dependency: true);
            }
            else if (_options.SpecFlowVersion >= new Version("3.0"))
            {
                InstallNuGetPackage(projectChanger, packagesFolder, "BoDi", dependency: true, packageVersion: "1.4.0-alpha1");
                InstallNuGetPackage(projectChanger, packagesFolder, "Gherkin", dependency: true, packageVersion: "6.0.0-beta1");
                InstallNuGetPackage(projectChanger, packagesFolder, "Utf8Json", "net45", dependency: true, packageVersion: "1.3.7");
                InstallNuGetPackage(projectChanger, packagesFolder, "System.ValueTuple", "netstandard1.0", dependency: true);
            }
            else if (_options.SpecFlowVersion >= new Version("2.3"))
            {
                InstallNuGetPackage(projectChanger, packagesFolder, "Newtonsoft.Json", dependency: true);
                InstallNuGetPackage(projectChanger, packagesFolder, "System.ValueTuple", "netstandard1.0", dependency: true);
            }
        }

        private string GetSpecFlowSourcePlatform()
        {
            var sourcePlatform =
                _options.SpecFlowVersion >= new Version("3.3") ? "net461" :
                _options.SpecFlowVersion >= new Version("2.0") ? "net45" :
                "net35";
            return sourcePlatform;
        }

        protected void InstallNuGetPackage(ProjectChanger projectChanger, string packagesFolder, string packageName,
            string sourcePlatform = "net45", string packageVersion = null, bool dependency = false)
        {
            var package = projectChanger.InstallNuGetPackage(packagesFolder, packageName, sourcePlatform, packageVersion, dependency);
            if (package != null)
                InstalledNuGetPackages.Add(package);
        }

        private void EnsureEmptyFolder(string folder)
        {
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
                return;
            }

            if (!Directory.GetFileSystemEntries(folder).Any())
                return;

            foreach (var subFolder in Directory.GetDirectories(folder))
            {
                try
                {
                    Directory.Delete(subFolder, true);
                }
                catch (Exception) { }
            }

            foreach (var file in Directory.GetFiles(folder))
            {
                try
                {
                    File.Delete(file);
                }
                catch (Exception) { }
            }

            Thread.Sleep(200);

            if (!Directory.GetFileSystemEntries(folder).Any())
                return;

            for (int i = 0; i < 3; i++)
            {
                if (Directory.Exists(folder))
                {
                    Exec(Path.Combine(folder, ".."), Environment.GetEnvironmentVariable("ComSpec"), "/C", "rmdir",
                        "/S", "/Q", folder);
                    Thread.Sleep(500);
                }
            }

            if (Directory.Exists(folder))
            {
                Directory.Delete(folder, true);
                Thread.Sleep(500);
            }
        }

        private void ExecNuGet(params string[] args)
        {
            Exec(_options.TargetFolder, ToolLocator.GetToolPath(ExternalTools.NuGet, _consoleWriteLine), args);
        }

        private int ExecGit(params string[] args)
        {
            return Exec(_options.TargetFolder, ToolLocator.GetToolPath(ExternalTools.Git, _consoleWriteLine), args);
        }

        public int ExecMsBuild(params string[] args)
        {
            return Exec(_options.TargetFolder, ToolLocator.GetToolPath(ExternalTools.MsBuild, _consoleWriteLine), args);
        }

        protected int Exec(string workingDirectory, string tool, params string[] args)
        {
            var arguments = string.Join(" ", args);
            _consoleWriteLine($"{tool} {arguments}");
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    WorkingDirectory = workingDirectory,
                    FileName = tool,
                    Arguments = arguments,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardError = true
                }
            };
            process.Start();
            var output = process.StandardError.ReadToEnd();
            if (!string.IsNullOrWhiteSpace(output))
                _consoleWriteLine(output);
            process.WaitForExit();

            return process.ExitCode;
        }

    }
}
