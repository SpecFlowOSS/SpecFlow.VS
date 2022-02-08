using TechTalk.SpecFlow.Infrastructure;

namespace SpecFlowConnector.SpecFlowProxies;

public class BindingRegistryFactoryBeforeV309022 : BindingRegistryFactoryVLatest
{
    public BindingRegistryFactoryBeforeV309022(IFileSystem fileSystem) : base(fileSystem)
    {
    }

    protected override IDefaultDependencyProvider CreateDependencyProvider(AssemblyLoadContext assemblyLoadContext) =>
        new SpecFlowDependencyProviderBeforeV3922(assemblyLoadContext);
}
