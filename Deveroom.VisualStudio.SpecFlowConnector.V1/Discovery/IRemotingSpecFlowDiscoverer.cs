using System;
using System.Linq;

namespace Deveroom.VisualStudio.SpecFlowConnector.Discovery
{
    interface IRemotingSpecFlowDiscoverer : IDisposable
    {
        string Discover(string testAssembly, string configFilePath);
    }
}
