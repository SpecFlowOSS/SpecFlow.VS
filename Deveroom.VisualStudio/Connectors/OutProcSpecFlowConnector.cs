using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Deveroom.VisualStudio.Configuration;
using Deveroom.VisualStudio.Diagonostics;
using Deveroom.VisualStudio.ProjectSystem;
using Deveroom.VisualStudio.SpecFlowConnector;
using Deveroom.VisualStudio.SpecFlowConnector.Models;

namespace Deveroom.VisualStudio.Connectors
{
    public class OutProcSpecFlowConnector
    {
        private const string ConnectorV1AnyCpu = @"V1\deveroom-specflow-v1.exe";
        private const string ConnectorV1X86 = @"V1\deveroom-specflow-v1.x86.exe";
        private const string ConnectorV2AnyCpu = @"V2\deveroom-specflow-v2.dll";
        private const string ConnectorV3AnyCpu = @"V3\deveroom-specflow-v3.dll";
        private const string GenerationCommandName = "generation";
        private const string BindingDiscoveryCommandName = "binding discovery";

        private readonly DeveroomConfiguration _configuration;
        private readonly IDeveroomLogger _logger;
        private readonly TargetFrameworkMoniker _targetFrameworkMoniker;
        private readonly string _extensionFolder;

        public OutProcSpecFlowConnector(DeveroomConfiguration configuration, IDeveroomLogger logger, TargetFrameworkMoniker targetFrameworkMoniker, string extensionFolder)
        {
            _configuration = configuration;
            _logger = logger;
            _targetFrameworkMoniker = targetFrameworkMoniker;
            _extensionFolder = extensionFolder;
        }

        private bool DebugConnector => _configuration.DebugConnector || Environment.GetEnvironmentVariable("DEVEROOM_DEBUGCONNECTOR") == "1";

        public DiscoveryResult RunDiscovery(string testAssemblyPath, string configFilePath)
        {
            var workingDirectory = Path.GetDirectoryName(testAssemblyPath);
            var arguments = new List<string>();
            var connectorPath = GetConnectorPath(arguments);
            arguments.Add("discovery");
            arguments.Add(testAssemblyPath);
            arguments.Add(configFilePath);
            if (DebugConnector)
                arguments.Add("--debug");

            if (!File.Exists(connectorPath))
                return new DiscoveryResult
                {
                    ErrorMessage = $"Error during binding discovery. Unable to find connector: {connectorPath}"
                };

            var result = ProcessHelper.RunProcess(workingDirectory, connectorPath, arguments, encoding: Encoding.UTF8);
            if (result.ExitCode != 0)
            {
                var errorMessage = result.HasErrors ? result.StandardError : "Unknown error.";

                return new DiscoveryResult
                {
                    ErrorMessage = GetDetailedErrorMessage(result, errorMessage, BindingDiscoveryCommandName)
                };
            }

            _logger.LogVerbose(result.StandardOut);

            var discoveryResult = JsonSerialization.DeserializeObjectWithMarker<DiscoveryResult>(result.StandardOut);
            if (discoveryResult.IsFailed)
                discoveryResult.ErrorMessage = GetDetailedErrorMessage(result, discoveryResult.ErrorMessage, BindingDiscoveryCommandName);

            return discoveryResult;
        }

        private string GetDetailedErrorMessage(ProcessHelper.RunProcessResult result, string errorMessage, string command)
        {
            var exitCode = result.ExitCode < 0 ? "<not executed>" : result.ExitCode.ToString();
            return $"Error during {command}. {Environment.NewLine}Command executed:{Environment.NewLine}  {result.CommandLine}{Environment.NewLine}Exit code: {exitCode}{Environment.NewLine}Message: {Environment.NewLine}{errorMessage}";
        }

        public GenerationResult RunGenerator(string featureFilePath, string configFilePath, string targetExtension, string targetNamespace, string projectFolder, string specFlowToolsFolder, string projectDefaultNamespace = null, bool saveResultToFile = false)
        {
            var workingDirectory = specFlowToolsFolder;
            var arguments = new List<string>();
            var connectorPath = GetConnectorPath(arguments);
            arguments.Add("generate");
            arguments.Add(featureFilePath);
            arguments.Add(configFilePath);
            arguments.Add(targetExtension);
            arguments.Add(targetNamespace);
            arguments.Add(projectFolder);
            arguments.Add(projectDefaultNamespace);
            if (saveResultToFile)
                arguments.Add("--save");
            if (DebugConnector)
                arguments.Add("--debug");
            var result = ProcessHelper.RunProcess(workingDirectory, connectorPath, arguments, encoding: Encoding.UTF8);
            if (result.ExitCode != 0)
            {
                var errorMessage = result.HasErrors ? result.StandardError : "Unknown error.";

                return new GenerationResult
                {
                    ErrorMessage = GetDetailedErrorMessage(result, errorMessage, GenerationCommandName)
                };
            }

            var generationResult = JsonSerialization.DeserializeObjectWithMarker<GenerationResult>(result.StandardOut);
            if (generationResult.FeatureFileCodeBehind == null && !generationResult.IsFailed)
                generationResult.ErrorMessage = "No code-behind information provided";

            if (generationResult.IsFailed)
            {
                generationResult.ErrorMessage =
                    GetDetailedErrorMessage(result, Environment.NewLine + generationResult.ErrorMessage, GenerationCommandName);
            }

            return generationResult;
        }

        private string GetConnectorPath(List<string> arguments)
        {
            var connectorsFolder = GetConnectorsFolder();

            //V3
            if (_targetFrameworkMoniker != null && 
                _targetFrameworkMoniker.IsNetCore && 
                _targetFrameworkMoniker.HasVersion &&
                _targetFrameworkMoniker.Version >= new Version(3,1))
            {
                arguments.Add("exec");
                arguments.Add(Path.Combine(connectorsFolder, ConnectorV3AnyCpu));
                return GetDotNetCommand();
            }

            //V2
            if (_targetFrameworkMoniker != null && 
                _targetFrameworkMoniker.IsNetCore)
            {
                arguments.Add("exec");
                //arguments.Add("--additionalprobingpath");
                //arguments.Add(Path.Combine(GetDotNetInstallLocation(), "sdk", "NuGetFallbackFolder"));
                //arguments.Add("--additionalprobingpath");
                //arguments.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nuget", "packages"));
                arguments.Add(Path.Combine(connectorsFolder, ConnectorV2AnyCpu));
                return GetDotNetCommand();
            }

            //V1
            string connectorName = ConnectorV1AnyCpu;
            if (_configuration.ProcessorArchitecture == ProcessorArchitectureSetting.X86)
                connectorName = ConnectorV1X86;

            return Path.Combine(connectorsFolder, connectorName);
        }

        private string GetDotNetInstallLocation()
        {
            var programFiles = Environment.GetEnvironmentVariable("ProgramW6432");
            if (_configuration.ProcessorArchitecture == ProcessorArchitectureSetting.X86)
                programFiles = Environment.GetEnvironmentVariable("ProgramFiles(x86)");
            if (string.IsNullOrEmpty(programFiles))
                programFiles = Environment.GetEnvironmentVariable("ProgramFiles");
            return Path.Combine(programFiles, "dotnet");
        }

        private string GetDotNetCommand()
        {
            return Path.Combine(GetDotNetInstallLocation(), "dotnet.exe");
        }

        private string GetConnectorsFolder()
        {
            var connectorsFolder = Path.Combine(_extensionFolder, "Connectors");
            if (Directory.Exists(connectorsFolder))
                return connectorsFolder;
            return _extensionFolder;
        }
    }
}
