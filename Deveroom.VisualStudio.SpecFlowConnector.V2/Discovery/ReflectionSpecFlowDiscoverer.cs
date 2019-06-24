using System;
using System.Reflection;
using McMaster.NETCore.Plugins;

namespace Deveroom.VisualStudio.SpecFlowConnector.Discovery
{
    internal class ReflectionSpecFlowDiscoverer : ISpecFlowDiscoverer
    {
        private readonly object _discovererObj;

        public ReflectionSpecFlowDiscoverer(PluginLoader pluginLoader, Type discovererType)
        {
            var discovererAssembly = pluginLoader.LoadAssemblyFromPath(discovererType.Assembly.Location);
            var discovererRemoteType = discovererAssembly.GetType(discovererType.FullName);
            _discovererObj = Activator.CreateInstance(discovererRemoteType);
        }

        public string Discover(Assembly testAssembly, string testAssemblyPath, string configFilePath)
        {
            return _discovererObj.ReflectionCallMethod<string>(nameof(Discover), new[] { typeof(Assembly), typeof(string), typeof(string) },
                testAssembly, testAssemblyPath, configFilePath);
        }

        public void Dispose()
        {
            _discovererObj.ReflectionCallMethod<object>(nameof(Dispose));
        }
    }
}