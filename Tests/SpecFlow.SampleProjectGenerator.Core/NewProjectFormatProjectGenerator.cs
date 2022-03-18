﻿using System;

namespace SpecFlow.SampleProjectGenerator;

public class NewProjectFormatProjectGenerator : ProjectGenerator
{
    public NewProjectFormatProjectGenerator(GeneratorOptions options, Action<string> consoleWriteLine) : base(options,
        consoleWriteLine)
    {
    }

    public override string PackagesFolder => GetPackagesFolder();

    public override string GetOutputAssemblyPath(string config = "Debug")
        => Path.Combine("bin", config, _options.TargetFramework, AssemblyFileName);

    protected override ProjectChanger CreateProjectChanger(string projectFilePath) =>
        new NewProjectFormatProjectChanger(projectFilePath);

    protected override string GetTemplatesFolder() => @"Templates\CS-NEW";

    protected override string GetPackagesFolder() =>
        Environment.ExpandEnvironmentVariables(@"%USERPROFILE%\.nuget\packages");

    protected override void InstallSpecFlowPackages(string packagesFolder, ProjectChanger projectChanger)
    {
        InstallNuGetPackage(projectChanger, packagesFolder, "SpecFlow.Tools.MsBuild.Generation", "net45",
            _options.SpecFlowPackageVersion);
    }

    protected override void SetSpecFlowUnitTestProvider(ProjectChanger projectChanger, string packagesFolder)
    {
        if (_options.SpecFlowVersion >= new Version("3.0.0"))
        {
            InstallNuGetPackage(projectChanger, packagesFolder, $"SpecFlow.{_options.UnitTestProvider}", "net45",
                _options.SpecFlowPackageVersion);
            return;
        }

        base.SetSpecFlowUnitTestProvider(projectChanger, packagesFolder);
    }

    protected override void BuildProject()
    {
        var exitCode = ExecDotNet("restore", "-s c:\\git\\SpecFlow.VS\\Tests\\ExternalPackages");
        if (exitCode != 0)
        {
            _consoleWriteLine($"dotnet restore exit code: {exitCode}");
            throw new Exception($"dotnet restore failed with exit code {exitCode}");
        }

        base.BuildProject();
    }

    protected override int ExecBuild() => ExecDotNet("build");
}
