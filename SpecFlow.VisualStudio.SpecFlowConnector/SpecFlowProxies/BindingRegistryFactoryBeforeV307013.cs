using TechTalk.SpecFlow.Infrastructure;

namespace SpecFlowConnector.SpecFlowProxies;

public class BindingRegistryFactoryBeforeV307013 : BindingRegistryFactoryBeforeV309022
{
    public BindingRegistryFactoryBeforeV307013(IFileSystem fileSystem) : base(fileSystem)
    {
    }

    protected override IDefaultDependencyProvider CreateDependencyProvider(AssemblyLoadContext assemblyLoadContext) =>
        new SpecFlowDependencyProviderBeforeV307013(assemblyLoadContext);
}
