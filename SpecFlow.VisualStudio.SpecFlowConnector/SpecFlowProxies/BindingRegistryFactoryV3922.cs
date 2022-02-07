using BoDi;
using TechTalk.SpecFlow;
using TechTalk.SpecFlow.Bindings;
using TechTalk.SpecFlow.Configuration;
using TechTalk.SpecFlow.Infrastructure;
using TechTalk.SpecFlow.Tracing;

namespace SpecFlowConnector.SpecFlowProxies;

public class BindingRegistryFactoryV3922 : BindingRegistryFactory<IObjectContainer>
{
    private readonly IFileSystem _fileSystem;

    public BindingRegistryFactoryV3922(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }

    protected override void RegisterTypeAs<TType, TInterface>(IObjectContainer globalContainer) 
        => globalContainer.RegisterTypeAs<TType, TInterface>();

    protected override IObjectContainer CreateGlobalContainer(AssemblyLoadContext assemblyLoadContext,
        Option<FileDetails> configFile, Assembly testAssembly)
    {
        var dependencyProvider = new SpecFlowV31DependencyProvider(assemblyLoadContext);
        var containerBuilder = new ContainerBuilderThatResetsTraceListener(dependencyProvider);
        IConfigurationLoader configurationLoader = new SpecFlowConfigurationLoader(configFile, _fileSystem);
        var configurationProvider = new DefaultRuntimeConfigurationProvider(configurationLoader);
        return containerBuilder.CreateGlobalContainer(testAssembly, configurationProvider);
    }

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
