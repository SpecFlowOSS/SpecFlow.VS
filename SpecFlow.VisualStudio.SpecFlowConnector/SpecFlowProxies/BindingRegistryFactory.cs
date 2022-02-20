using TechTalk.SpecFlow.Configuration;
using TechTalk.SpecFlow.Infrastructure;

namespace SpecFlowConnector.SpecFlowProxies;

public abstract class BindingRegistryFactory : IBindingRegistryFactory
{
    protected ILogger Log;

    protected BindingRegistryFactory(ILogger log)
    {
        Log = log;
    }

    public IBindingRegistryAdapter GetBindingRegistry(AssemblyLoadContext assemblyLoadContext,
        Assembly testAssembly, Option<FileDetails> configFile) =>
        CreateObjectContainer(
                testAssembly,
                assemblyLoadContext
                    .Map(CreateDependencyProvider)
                    .Map(CreateContainerBuilder),
                configFile
                    .Map<Option<FileDetails>,
                        object>(CreateConfigurationLoader)
                    .Map(CreateConfigurationProvider)
            )
            .Tie(container => CreateTestRunner(container, testAssembly))
            .Map(container => ResolveBindingRegistry(testAssembly, container))
            .Map(AdaptBindingRegistry);

    protected abstract object CreateObjectContainer(Assembly testAssembly, object containerBuilder,
        IRuntimeConfigurationProvider configurationProvider);

    protected abstract IDefaultDependencyProvider CreateDependencyProvider(AssemblyLoadContext assemblyLoadContext);

    protected abstract object CreateContainerBuilder(IDefaultDependencyProvider dependencyProvider);

    protected abstract object CreateConfigurationLoader(Option<FileDetails> configFile);

    protected abstract IRuntimeConfigurationProvider CreateConfigurationProvider(
        object configurationLoader);

    protected abstract void CreateTestRunner(object globalContainer, Assembly testAssembly);
    protected abstract object ResolveBindingRegistry(Assembly testAssembly, object globalContainer);

    protected abstract IBindingRegistryAdapter AdaptBindingRegistry(object bindingRegistry);
}
