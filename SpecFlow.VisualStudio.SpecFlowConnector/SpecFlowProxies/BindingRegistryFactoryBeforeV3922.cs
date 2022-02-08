using TechTalk.SpecFlow.Infrastructure;

namespace SpecFlowConnector.SpecFlowProxies;

public class BindingRegistryFactoryBeforeV3922 : BindingRegistryFactoryVLatest
{
    public BindingRegistryFactoryBeforeV3922(IFileSystem fileSystem) : base(fileSystem)
    {
    }

    protected override IDefaultDependencyProvider CreateDependencyProvider(AssemblyLoadContext assemblyLoadContext) =>
        new SpecFlowDependencyProviderBeforeV3922(assemblyLoadContext);
}
