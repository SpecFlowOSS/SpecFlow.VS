namespace SpecFlowConnector.SpecFlowProxies;

public interface IBindingRegistryAdapter
{
    IEnumerable<StepDefinitionBindingAdapter> GetStepDefinitions(AssemblyLoadContext assemblyLoadContext,
        Option<FileDetails> configFile, Assembly testAssembly);
}
