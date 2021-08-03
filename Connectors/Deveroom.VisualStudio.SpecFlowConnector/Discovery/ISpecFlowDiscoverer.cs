using System;
using System.Reflection;

namespace Deveroom.VisualStudio.SpecFlowConnector.Discovery
{
    interface ISpecFlowDiscoverer : IDisposable
    {
        string Discover(Assembly testAssembly, string testAssemblyPath, string configFilePath);
    }
}
