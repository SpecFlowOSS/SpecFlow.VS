using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Deveroom.VisualStudio.SpecFlowConnector.Discovery.V3000;
using TechTalk.SpecFlow;

namespace Deveroom.VisualStudio.SpecFlowConnector.Discovery
{
    public class VersionSelectorDiscoverer : ISpecFlowDiscoverer
    {
        private ISpecFlowDiscoverer _discoverer;

        public string Discover(Assembly testAssembly, string testAssemblyPath, string configFilePath)
        {
            if (_discoverer == null)
                _discoverer = CreateDiscoverer();

            return _discoverer.Discover(testAssembly, testAssemblyPath, configFilePath);
        }

        public void Dispose()
        {
            _discoverer?.Dispose();
            _discoverer = null;
        }

        private ISpecFlowDiscoverer CreateDiscoverer()
        {
            var specFlowVersion = GetSpecFlowVersion();

            var discovererType = typeof(SpecFlowV3000P220Discoverer); // assume recent version
            if (specFlowVersion != null)
            {
                var versionNumber =
                    ((specFlowVersion.FileMajorPart * 100) + specFlowVersion.FileMinorPart) * 1000 + specFlowVersion.FileBuildPart;

                if (versionNumber >= 3_00_220)
                    discovererType = typeof(SpecFlowV3000P220Discoverer);
                else if (versionNumber >= 3_00_000)
                    discovererType = typeof(SpecFlowV3000Discoverer);
            }

            return (ISpecFlowDiscoverer)Activator.CreateInstance(discovererType);
        }

        private FileVersionInfo GetSpecFlowVersion()
        {
            var specFlowAssembly = typeof(ScenarioContext).Assembly;
            var specFlowAssemblyPath = specFlowAssembly.Location;
            var fileVersionInfo = File.Exists(specFlowAssemblyPath) ? FileVersionInfo.GetVersionInfo(specFlowAssemblyPath) : null;
            return fileVersionInfo;
        }
    }
}
