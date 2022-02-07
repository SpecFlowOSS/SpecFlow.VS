using TechTalk.SpecFlow.Bindings;
using TechTalk.SpecFlow.Configuration;
using TechTalk.SpecFlow.Infrastructure;

namespace SpecFlowConnector.SpecFlowProxies;

public abstract class BindingRegistryFactory<TGlobalContainer> : IBindingRegistryFactory
{
    public IBindingRegistry GetBindingRegistry(AssemblyLoadContext assemblyLoadContext,
        Assembly testAssembly, Option<FileDetails> configFile) =>
        CreateObjectContainer(testAssembly, assemblyLoadContext
                .Map(CreateDependencyProvider)
                .Map(CreateContainerBuilder), configFile
                .Map<Option<FileDetails>, IConfigurationLoader>(CreateConfigurationLoader)
                .Map(CreateConfigurationProvider))
            .Tie(RegisterTypeAs<NoInvokeDependencyProvider.NullBindingInvoker, IBindingInvoker>)
            .Tie(container => CreateTestRunner(container, testAssembly))
            .Map(container => ResolveBindingRegistry(testAssembly, container));

    protected abstract IDefaultDependencyProvider CreateDependencyProvider(AssemblyLoadContext assemblyLoadContext);
    protected abstract ContainerBuilder CreateContainerBuilder(IDefaultDependencyProvider dependencyProvider);
    protected abstract IConfigurationLoader CreateConfigurationLoader(Option<FileDetails> configFile);

    protected abstract IRuntimeConfigurationProvider CreateConfigurationProvider(
        IConfigurationLoader configurationLoader);

    protected abstract TGlobalContainer CreateObjectContainer(Assembly testAssembly, ContainerBuilder containerBuilder,
        IRuntimeConfigurationProvider configurationProvider);

    protected abstract void RegisterTypeAs<TType, TInterface>(TGlobalContainer globalContainer)
        where TType : class, TInterface;

    protected abstract void CreateTestRunner(TGlobalContainer globalContainer, Assembly testAssembly);
    protected abstract IBindingRegistry ResolveBindingRegistry(Assembly testAssembly, TGlobalContainer globalContainer);
}
