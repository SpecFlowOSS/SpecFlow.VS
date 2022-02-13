using BoDi;
using TechTalk.SpecFlow;
using TechTalk.SpecFlow.Bindings;
using TechTalk.SpecFlow.Configuration;
using TechTalk.SpecFlow.Infrastructure;

namespace SpecFlowConnector.SpecFlowProxies;

public class BindingRegistryFactoryVLatest : BindingRegistryFactory
{
    protected readonly IFileSystem FileSystem;

    public BindingRegistryFactoryVLatest(IFileSystem fileSystem)
    {
        FileSystem = fileSystem;
    }

    protected override IDefaultDependencyProvider CreateDependencyProvider(AssemblyLoadContext assemblyLoadContext) =>
        new SpecFlowDependencyProviderVLatest(assemblyLoadContext);

    protected override IConfigurationLoader CreateConfigurationLoader(Option<FileDetails> configFile) =>
        new SpecFlowConfigurationLoader(configFile, FileSystem);

    protected override IRuntimeConfigurationProvider
        CreateConfigurationProvider(IConfigurationLoader configurationLoader) =>
        new DefaultRuntimeConfigurationProvider(configurationLoader);

    protected override ContainerBuilder CreateContainerBuilder(IDefaultDependencyProvider dependencyProvider) =>
        new ContainerBuilderThatResetsTraceListener(dependencyProvider);

    protected override object CreateObjectContainer(Assembly testAssembly, ContainerBuilder containerBuilder,
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
}