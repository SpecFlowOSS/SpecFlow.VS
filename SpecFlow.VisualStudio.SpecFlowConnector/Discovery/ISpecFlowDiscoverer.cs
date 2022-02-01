namespace SpecFlow.VisualStudio.SpecFlowConnector.Discovery;

public interface ISpecFlowDiscoverer : IDisposable
{
    DiscoveryResult Discover(Assembly testAssembly, ConfigFile config);
}