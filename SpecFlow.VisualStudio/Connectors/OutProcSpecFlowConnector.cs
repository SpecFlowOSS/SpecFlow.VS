using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using SpecFlow.VisualStudio.Configuration;
using SpecFlow.VisualStudio.Diagnostics;
using SpecFlow.VisualStudio.ProjectSystem;
using SpecFlow.VisualStudio.SpecFlowConnector;
using SpecFlow.VisualStudio.SpecFlowConnector.Models;

namespace SpecFlow.VisualStudio.Connectors
{
    public class OutProcSpecFlowConnector
    {
        private const string ConnectorV1AnyCpu = @"V1\specflow-vs.exe";
        private const string ConnectorV1X86 = @"V1\specflow-vs-x86.exe";
        private const string ConnectorV2NetCore21 = @"V2-netcoreapp2.1\specflow-vs.dll";
        private const string ConnectorV2NetCore31 = @"V2-netcoreapp3.1\specflow-vs.dll";
        private const string ConnectorV2Net50 = @"V2-net5.0\specflow-vs.dll";
        private const string ConnectorV2Net60 = @"V2-net6.0\specflow-vs.dll";
        private const string ConnectorV3NetCore21 = @"V3-netcoreapp2.1\specflow-vs.dll";
        private const string ConnectorV3NetCore31 = @"V3-netcoreapp3.1\specflow-vs.dll";
        private const string ConnectorV3Net50 = @"V3-net5.0\specflow-vs.dll";
        private const string ConnectorV3Net60 = @"V3-net6.0\specflow-vs.dll";
        private const string GenerationCommandName = "generation";
        private const string BindingDiscoveryCommandName = "binding discovery";

        private readonly DeveroomConfiguration _configuration;
        private readonly IDeveroomLogger _logger;
        private readonly TargetFrameworkMoniker _targetFrameworkMoniker;
        private readonly string _extensionFolder;
        private readonly ProcessorArchitectureSetting _processorArchitecture;
        private readonly NuGetVersion _specFlowVersion;
        private Version _netCoreVersion;

        public OutProcSpecFlowConnector(DeveroomConfiguration configuration, IDeveroomLogger logger,
            TargetFrameworkMoniker targetFrameworkMoniker, string extensionFolder,
            ProcessorArchitectureSetting processorArchitecture, NuGetVersion specFlowVersion)
        {
            _configuration = configuration;
            _logger = logger;
            _targetFrameworkMoniker = targetFrameworkMoniker;
            _extensionFolder = extensionFolder;
            _processorArchitecture = processorArchitecture;
            _specFlowVersion = specFlowVersion;
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

            _logger.LogVerbose($"{workingDirectory}>{connectorPath} {string.Join(" ", arguments)}");

#if DEBUG
            _logger.LogVerbose(result.StandardOut);
#endif

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

        private bool IsNetCoreVersionOrLater(int major, int minor)
        {
            if (!_targetFrameworkMoniker.IsNetCore) 
                return false;

            var version = new Version(major, minor);
            return NetCoreVersion >= version;
        }

        private Version NetCoreVersion => _netCoreVersion ??= GetNetCoreVersion();

        private Version GetNetCoreVersion()
        {
            var dotnet = GetDotNetCommand();
            var result = ProcessHelper.RunProcess("./", dotnet, new[] {"--version"}, encoding: Encoding.UTF8);
            var resultParts = result.StandardOut.Split('-');
            var dotnetVersion = new Version(resultParts[0]);
            return dotnetVersion;
        }

        private string GetConnectorPath(List<string> arguments)
        {
            var connectorsFolder = GetConnectorsFolder();

            if (_specFlowVersion != null && _specFlowVersion.Version >= new Version(3, 9, 22))
            {
                //V3-net6.0
                if (IsNetCoreVersionOrLater(6, 0))
                    return GetDotNetExecCommand(arguments, connectorsFolder, ConnectorV3Net60);             
                
                //V3-net5.0
                if (IsNetCoreVersionOrLater(5, 0))
                    return GetDotNetExecCommand(arguments, connectorsFolder, ConnectorV3Net50);

                //V3-netcoreapp3.1
                if (IsNetCoreVersionOrLater(3, 1))
                    return GetDotNetExecCommand(arguments, connectorsFolder, ConnectorV3NetCore31);

                //V3-netcoreapp2.1
                if (IsNetCoreVersionOrLater(2, 0))
                    return GetDotNetExecCommand(arguments, connectorsFolder, ConnectorV3NetCore21);
            }

            //V2-net6.0
            if (IsNetCoreVersionOrLater(6, 0))
                return GetDotNetExecCommand(arguments, connectorsFolder, ConnectorV2Net60);

            //V2-net5.0
            if (IsNetCoreVersionOrLater(5, 0))
                return GetDotNetExecCommand(arguments, connectorsFolder, ConnectorV2Net50);

            //V2-netcoreapp3.1
            if (IsNetCoreVersionOrLater(3, 1))
                return GetDotNetExecCommand(arguments, connectorsFolder, ConnectorV2NetCore31);

            //V2-netcoreapp2.1
            if (IsNetCoreVersionOrLater(2, 0))
                return GetDotNetExecCommand(arguments, connectorsFolder, ConnectorV2NetCore21);

            //V1
            string connectorName = ConnectorV1AnyCpu;
            if (_processorArchitecture == ProcessorArchitectureSetting.X86)
                connectorName = ConnectorV1X86;

            return Path.Combine(connectorsFolder, connectorName);
        }

        private string GetDotNetInstallLocation()
        {
            var programFiles = Environment.GetEnvironmentVariable("ProgramW6432");
            if (_processorArchitecture == ProcessorArchitectureSetting.X86)
                programFiles = Environment.GetEnvironmentVariable("ProgramFiles(x86)");
            if (string.IsNullOrEmpty(programFiles))
                programFiles = Environment.GetEnvironmentVariable("ProgramFiles");
            return Path.Combine(programFiles, "dotnet");
        }

        private string GetDotNetExecCommand(List<string> arguments, string executableFolder, string executableFile)
        {
            arguments.Add("exec");
            arguments.Add(Path.Combine(executableFolder, executableFile));
            return GetDotNetCommand();
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
