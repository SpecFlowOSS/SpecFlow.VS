using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using SpecFlow.VisualStudio.Common;
using McMaster.NETCore.Plugins;

namespace SpecFlow.VisualStudio.SpecFlowConnector.Discovery
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
            var pluginLoader = PluginLoader.CreateFromAssemblyFile(_options.AssemblyFilePath, PluginLoaderOptions.IncludeCompileLibraries);
            var targetFolder = Path.GetDirectoryName(_options.AssemblyFilePath);
            if (targetFolder == null)
                return null;

            var connectorFolder = Path.GetDirectoryName(typeof(ConsoleRunner).Assembly.GetLocalCodeBase());
            Debug.Assert(connectorFolder != null);

            using (var discoverer = new ReflectionSpecFlowDiscoverer(GetLoadContext(pluginLoader), 
                typeof(VersionSelectorDiscoverer)))
            {
                var testAssembly = pluginLoader.LoadDefaultAssembly();
                return discoverer.Discover(testAssembly, _options.AssemblyFilePath, _options.ConfigFilePath);
            }
        }

        private AssemblyLoadContext GetLoadContext(PluginLoader pluginLoader)
        {
            // ReSharper disable once PossibleNullReferenceException
            return (AssemblyLoadContext)pluginLoader.GetType()
                .GetField("_context", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(pluginLoader);
        }
    }
}
