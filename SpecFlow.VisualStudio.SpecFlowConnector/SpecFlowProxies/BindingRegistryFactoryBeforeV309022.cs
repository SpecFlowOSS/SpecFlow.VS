using TechTalk.SpecFlow.Infrastructure;

namespace SpecFlowConnector.SpecFlowProxies;

public class BindingRegistryFactoryBeforeV309022 : BindingRegistryFactoryVLatest
{
    public BindingRegistryFactoryBeforeV309022(ILogger log) : base(log)
    {
    }

    protected override IDefaultDependencyProvider CreateDependencyProvider(AssemblyLoadContext assemblyLoadContext) =>
        new SpecFlowDependencyProviderBeforeV309022(assemblyLoadContext);
}
