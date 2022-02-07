using TechTalk.SpecFlow.Bindings;

namespace SpecFlowConnector.SpecFlowProxies;

public class BindingRegistryAdapterAdapter : IBindingRegistryAdapter
{
    private readonly IBindingRegistryFactory _bindingRegistryFactory;


    public BindingRegistryAdapterAdapter(IBindingRegistryFactory bindingRegistryFactory)
    {
        _bindingRegistryFactory = bindingRegistryFactory;
    }

    public IEnumerable<StepDefinitionBindingAdapter> GetStepDefinitions(AssemblyLoadContext assemblyLoadContext,
        Option<FileDetails> configFile, Assembly testAssembly)
    {
        var bindingRegistry = _bindingRegistryFactory.GetBindingRegistry(assemblyLoadContext, testAssembly, configFile);
        var stepDefinitionBindings = bindingRegistry.GetStepDefinitions();
        return stepDefinitionBindings
            .Select(Adapt);
    }

    protected StepDefinitionBindingAdapter Adapt(IStepDefinitionBinding sd) 
        => new StepDefinitionBindingAdapter(sd);
}
