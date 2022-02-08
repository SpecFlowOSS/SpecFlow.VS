using BoDi;
using TechTalk.SpecFlow;
using TechTalk.SpecFlow.Bindings;
using TechTalk.SpecFlow.Configuration;
using TechTalk.SpecFlow.Infrastructure;
using TechTalk.SpecFlow.Tracing;

namespace SpecFlowConnector.SpecFlowProxies;

public class BindingRegistryFactoryVLatest : BindingRegistryFactory<IObjectContainer>
{
    private readonly IFileSystem _fileSystem;

    public BindingRegistryFactoryVLatest(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }

    protected override void RegisterTypeAs<TType, TInterface>(IObjectContainer globalContainer) 
        => globalContainer.RegisterTypeAs<TType, TInterface>();

    protected override IDefaultDependencyProvider CreateDependencyProvider(AssemblyLoadContext assemblyLoadContext) =>  new SpecFlowDependencyProviderVLatest(assemblyLoadContext);

    protected override IConfigurationLoader CreateConfigurationLoader(Option<FileDetails> configFile) => new SpecFlowConfigurationLoader(configFile, _fileSystem);
    protected override IRuntimeConfigurationProvider CreateConfigurationProvider(IConfigurationLoader configurationLoader) => new DefaultRuntimeConfigurationProvider(configurationLoader);

    protected override ContainerBuilder CreateContainerBuilder(IDefaultDependencyProvider dependencyProvider) => new ContainerBuilderThatResetsTraceListener(dependencyProvider);

    protected override IObjectContainer CreateObjectContainer(Assembly testAssembly, ContainerBuilder containerBuilder,
        IRuntimeConfigurationProvider configurationProvider) =>
        containerBuilder.CreateGlobalContainer(testAssembly, configurationProvider);

    private class ContainerBuilderThatResetsTraceListener : ContainerBuilder
    {
        public ContainerBuilderThatResetsTraceListener(IDefaultDependencyProvider defaultDependencyProvider = null) :
            base(defaultDependencyProvider)
        {
        }

        public override IObjectContainer CreateTestThreadContainer(IObjectContainer globalContainer)
        {
            var testThreadContainer = base.CreateTestThreadContainer(globalContainer);
            testThreadContainer.ReflectionRegisterTypeAs<NullListener, ITraceListener>();
            return testThreadContainer;
        }
    }

    protected override void CreateTestRunner(IObjectContainer globalContainer, Assembly testAssembly)
    {
        var testRunnerManager = (TestRunnerManager)globalContainer.Resolve<ITestRunnerManager>();

        testRunnerManager.Initialize(testAssembly);
        testRunnerManager.CreateTestRunner(0);
    }

    protected override IBindingRegistry ResolveBindingRegistry(Assembly testAssembly, IObjectContainer globalContainer)
        => globalContainer.Resolve<IBindingRegistry>();
}
