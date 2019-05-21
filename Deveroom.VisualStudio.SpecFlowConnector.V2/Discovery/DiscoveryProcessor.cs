using System;
using System.Diagnostics;
using System.IO;
using Deveroom.VisualStudio.Common;
using Deveroom.VisualStudio.SpecFlowConnector.Discovery.V3000;

namespace Deveroom.VisualStudio.SpecFlowConnector.Discovery
{
    public class DiscoveryProcessor
    {
        private readonly DiscoveryOptions _options;

        public DiscoveryProcessor(DiscoveryOptions options)
        {
            _options = options;
        }

        public string Process()
        {
            var loadContext = LoadContextHelper.CreateLoadContext(_options.AssemblyFilePath);
            var targetFolder = Path.GetDirectoryName(_options.AssemblyFilePath);
            if (targetFolder == null)
                return null;

            var connectorFolder = Path.GetDirectoryName(typeof(ConsoleRunner).Assembly.GetLocalCodeBase());
            Debug.Assert(connectorFolder != null);

            var specFlowVersion = GetSpecFlowVersion();
            var discovererType = typeof(SpecFlowV3000Discoverer);
            if (specFlowVersion != null)
                switch (specFlowVersion.FileMajorPart * 1000 + specFlowVersion.FileMinorPart * 10)
                {
                    case 3000:
                        discovererType = typeof(SpecFlowV3000Discoverer);
                        break;
                }

            using (var discoverer = new ReflectionSpecFlowDiscoverer(loadContext, discovererType))
            {
                var testAssembly = loadContext.LoadFromAssemblyPath(_options.AssemblyFilePath);
                return discoverer.Discover(testAssembly, _options.AssemblyFilePath, _options.ConfigFilePath);
            }
        }

        private FileVersionInfo GetSpecFlowVersion()
        {
            //TODO: find a better way, because for .NET Core the dependencies are typically not in the output folder. Idea: check 'deps.json' file, or plugins, like 'TechTalk.SpecFlow.NUnit.SpecFlowPlugin.dll'
            var specFlowAssemblyPath = Path.Combine(_options.TargetFolder, "TechTalk.SpecFlow.dll");
            var fileVersionInfo = File.Exists(specFlowAssemblyPath) ? FileVersionInfo.GetVersionInfo(specFlowAssemblyPath) : null;
            return fileVersionInfo;
        }
    }
}
