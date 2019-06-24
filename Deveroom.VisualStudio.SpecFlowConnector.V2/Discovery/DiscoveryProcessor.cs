using System;
using System.Diagnostics;
using System.IO;
using Deveroom.VisualStudio.Common;
using McMaster.NETCore.Plugins;

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
            var loadContext = PluginLoader.CreateFromAssemblyFile(_options.AssemblyFilePath, PluginLoaderOptions.IncludeCompileLibraries);
            var targetFolder = Path.GetDirectoryName(_options.AssemblyFilePath);
            if (targetFolder == null)
                return null;

            var connectorFolder = Path.GetDirectoryName(typeof(ConsoleRunner).Assembly.GetLocalCodeBase());
            Debug.Assert(connectorFolder != null);

            using (var discoverer = new ReflectionSpecFlowDiscoverer(loadContext, 
                typeof(VersionSelectorDiscoverer)))
            {
                var testAssembly = loadContext.LoadDefaultAssembly();
                return discoverer.Discover(testAssembly, _options.AssemblyFilePath, _options.ConfigFilePath);
            }
        }
    }
}
