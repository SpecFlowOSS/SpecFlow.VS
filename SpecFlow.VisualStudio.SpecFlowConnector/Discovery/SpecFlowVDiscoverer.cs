namespace SpecFlowConnector.Discovery;

public class SpecFlowDiscoverer {
    
    public DiscoveryResult Discover(
        IBindingRegistryProxy bindingRegistry, 
        Assembly testAssembly,
        Option<FileDetails> configFile)
    {
        bindingRegistry.GetStepDefinitions(testAssembly, configFile);
        return new DiscoveryResult();
    }
}
