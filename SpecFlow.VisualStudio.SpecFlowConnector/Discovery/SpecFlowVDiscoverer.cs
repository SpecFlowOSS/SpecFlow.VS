namespace SpecFlow.VisualStudio.SpecFlowConnector.Discovery;

public abstract class SpecFlowVDiscoverer :ISpecFlowDiscoverer {
    public void Dispose()
    {
    }

    public DiscoveryResult Discover(Assembly testAssembly, ConfigFile config) => throw new NotImplementedException();
}
