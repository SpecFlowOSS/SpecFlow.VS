using System.Diagnostics;
using System.IO;
using Deveroom.VisualStudio.SpecFlowConnector.AppDomainHelper;
using Deveroom.VisualStudio.SpecFlowConnector.Discovery.V1090;
using Deveroom.VisualStudio.SpecFlowConnector.Discovery.V2000;
using Deveroom.VisualStudio.SpecFlowConnector.Discovery.V2010;
using Deveroom.VisualStudio.SpecFlowConnector.Discovery.V2020;
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
            using (AssemblyHelper.SubscribeResolveForAssembly(_options.AssemblyFilePath))
            {
                var appDomain = new AppDomainManager(_options.AssemblyFilePath, _options.ConfigFilePath, false, null);

                var specFlowVersion = GetSpecFlowVersion();
                var discovererType = typeof(SpecFlowV2020Discoverer);
                if (specFlowVersion != null)
                    switch (specFlowVersion.FileMajorPart * 1000 + specFlowVersion.FileMinorPart * 10)
                    {
                        case 1090:
                            discovererType = typeof(SpecFlowV1090Discoverer);
                            break;
                        case 2000:
                            discovererType = typeof(SpecFlowV2000Discoverer);
                            break;
                        case 2010:
                            discovererType = typeof(SpecFlowV2010Discoverer);
                            break;
                        case 3000:
                            discovererType = specFlowVersion.FileBuildPart >= 220 ? 
                                typeof(SpecFlowV3000P220Discoverer) : 
                                typeof(SpecFlowV3000Discoverer);
                            break;
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
