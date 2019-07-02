using System.Diagnostics;
using System.IO;
using Deveroom.VisualStudio.SpecFlowConnector.AppDomainHelper;
using Deveroom.VisualStudio.SpecFlowConnector.Discovery.V19;
using Deveroom.VisualStudio.SpecFlowConnector.Discovery.V20;
using Deveroom.VisualStudio.SpecFlowConnector.Discovery.V21;
using Deveroom.VisualStudio.SpecFlowConnector.Discovery.V22;
using Deveroom.VisualStudio.SpecFlowConnector.Discovery.V30;

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
            using (AssemblyHelper.SubscribeResolveForAssembly(_options.AssemblyFilePath))
            {
                var appDomain = new AppDomainManager(_options.AssemblyFilePath, _options.ConfigFilePath, false, null);

                var specFlowVersion = GetSpecFlowVersion();
                var discovererType = typeof(SpecFlowV30P220Discoverer); // assume recent version
                if (specFlowVersion != null)
                {
                    var versionNumber =
                        ((specFlowVersion.FileMajorPart * 100) + specFlowVersion.FileMinorPart) * 1000 + specFlowVersion.FileBuildPart;

                    if (versionNumber >= 3_00_220)
                        discovererType = typeof(SpecFlowV30P220Discoverer);
                    else if (versionNumber >= 3_00_000)
                        discovererType = typeof(SpecFlowV30Discoverer);
                    else if (versionNumber >= 2_02_000)
                        discovererType = typeof(SpecFlowV22Discoverer);
                    else if (versionNumber >= 2_01_000)
                        discovererType = typeof(SpecFlowV21Discoverer);
                    else if (versionNumber >= 2_00_000)
                        discovererType = typeof(SpecFlowV20Discoverer);
                    else if (versionNumber >= 1_09_000)
                        discovererType = typeof(SpecFlowV19Discoverer);
                }

                appDomain.CreateObjectFrom<AssemblyHelper>(typeof(AssemblyHelper).Assembly.Location, typeof(AssemblyHelper).FullName, _options.TargetFolder);
                appDomain.CreateObjectFrom<AssemblyHelper>(typeof(AssemblyHelper).Assembly.Location, typeof(AssemblyHelper).FullName, _options.ConnectorFolder);
                using (var discoverer = appDomain.CreateObject<IRemotingSpecFlowDiscoverer>(discovererType.Assembly.GetName(), discovererType.FullName))
                {
                    return discoverer.Discover(_options.AssemblyFilePath, _options.ConfigFilePath);
                }
            }
        }

        private FileVersionInfo GetSpecFlowVersion()
        {
            var specFlowAssemblyPath = Path.Combine(_options.TargetFolder, "TechTalk.SpecFlow.dll");
            var fileVersionInfo = File.Exists(specFlowAssemblyPath) ? FileVersionInfo.GetVersionInfo(specFlowAssemblyPath) : null;
            return fileVersionInfo;
        }
    }
}
