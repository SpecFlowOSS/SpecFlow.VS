using TechTalk.SpecFlow;
using TechTalk.SpecFlow.Bindings;
using TechTalk.SpecFlow.Configuration;
using TechTalk.SpecFlow.Infrastructure;

namespace SpecFlowConnector.SpecFlowProxies;

public class BindingRegistryFactoryVLatest : BindingRegistryFactory
{
    public BindingRegistryFactoryVLatest(ILogger log) : base(log)
    {
    }

    protected override IDefaultDependencyProvider CreateDependencyProvider(AssemblyLoadContext assemblyLoadContext) =>
        new SpecFlowDependencyProviderVLatest(assemblyLoadContext);

    protected override object CreateConfigurationLoader(Option<FileDetails> configFile) =>
        new SpecFlowConfigurationLoader(configFile);

    protected override IRuntimeConfigurationProvider
        CreateConfigurationProvider(object configurationLoader) =>
        CreateConfigurationProvider((IConfigurationLoader) configurationLoader);

    protected virtual IRuntimeConfigurationProvider
        CreateConfigurationProvider(IConfigurationLoader configurationLoader) =>
        new DefaultRuntimeConfigurationProvider(configurationLoader);

    protected override object CreateContainerBuilder(IDefaultDependencyProvider dependencyProvider) =>
        new ContainerBuilderThatResetsTraceListener(dependencyProvider);

    protected override object CreateObjectContainer(Assembly testAssembly, object containerBuilder,
        IRuntimeConfigurationProvider configurationProvider) =>
        CreateObjectContainer(testAssembly, (ContainerBuilder) containerBuilder, configurationProvider);

    protected virtual object CreateObjectContainer(Assembly testAssembly, ContainerBuilder containerBuilder,
        IRuntimeConfigurationProvider configurationProvider) =>
        containerBuilder.CreateGlobalContainer(testAssembly, configurationProvider);

    protected override void CreateTestRunner(object globalContainer, Assembly testAssembly)
        => CreateTestRunner((IObjectContainer) globalContainer, testAssembly);

    protected virtual void CreateTestRunner(IObjectContainer globalContainer, Assembly testAssembly)
    {
        var testRunnerManager = (TestRunnerManager) globalContainer.Resolve<ITestRunnerManager>();

        testRunnerManager.Initialize(testAssembly);
        testRunnerManager.CreateTestRunner(0);
    }

    protected override IBindingRegistry ResolveBindingRegistry(Assembly testAssembly, object globalContainer)
        => ResolveBindingRegistry(testAssembly, (IObjectContainer) globalContainer);

    protected virtual IBindingRegistry ResolveBindingRegistry(Assembly testAssembly, IObjectContainer globalContainer)
        => globalContainer.Resolve<IBindingRegistry>();

    protected override IBindingRegistryAdapter AdaptBindingRegistry(object bindingRegistry)
        => AdaptBindingRegistry((IBindingRegistry) bindingRegistry);

    protected virtual IBindingRegistryAdapter AdaptBindingRegistry(IBindingRegistry bindingRegistry)
        => new BindingRegistryAdapterVLatest(bindingRegistry);
}
