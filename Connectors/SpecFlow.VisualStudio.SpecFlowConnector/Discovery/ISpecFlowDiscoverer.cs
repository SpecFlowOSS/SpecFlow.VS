using System;
using System.Reflection;
using SpecFlow.VisualStudio.SpecFlowConnector.Models;

namespace SpecFlow.VisualStudio.SpecFlowConnector.Discovery
{
    interface ISpecFlowDiscoverer : IDisposable
    {
        string Discover(Assembly testAssembly, string testAssemblyPath, string configFilePath);
    }

    interface IDiscoveryResultDiscoverer : ISpecFlowDiscoverer
    {
        DiscoveryResult DiscoverInternal(Assembly testAssembly, string testAssemblyPath, string configFilePath);
    }
}
