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

            using (var discoverer = new ReflectionSpecFlowDiscoverer(loadContext, 
                typeof(VersionSelectorDiscoverer)))
            {
                var testAssembly = loadContext.LoadFromAssemblyPath(_options.AssemblyFilePath);
                return discoverer.Discover(testAssembly, _options.AssemblyFilePath, _options.ConfigFilePath);
            }
        }
    }
}
