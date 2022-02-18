using TechTalk.SpecFlow;
using TechTalk.SpecFlow.Bindings;
using TechTalk.SpecFlow.Bindings.Discovery;
using TechTalk.SpecFlow.Configuration;
using TechTalk.SpecFlow.Infrastructure;

namespace SpecFlowConnector.SpecFlowProxies;

public class BindingRegistryFactoryBeforeV300225 : BindingRegistryFactoryBeforeV301062
{
    private IDefaultDependencyProvider _defaultDependencyProvider = null!;

    public BindingRegistryFactoryBeforeV300225(ILogger log) : base(log)
    {
    }

    protected override object CreateConfigurationLoader(Option<FileDetails> configFile) =>
        new SpecFlow21ConfigurationLoader(configFile);

    protected override IDefaultDependencyProvider CreateDependencyProvider(AssemblyLoadContext assemblyLoadContext)
    {
        _defaultDependencyProvider = new SpecFlowDependencyProviderBeforeV300213(assemblyLoadContext);
        return _defaultDependencyProvider;
    }

    protected override object CreateObjectContainer(
        Assembly testAssembly,
        ContainerBuilder containerBuilder,
        IRuntimeConfigurationProvider configurationProvider)
    {
        var globalContainer = containerBuilder.ReflectionCallMethod<object>(
            nameof(ContainerBuilder.CreateGlobalContainer),
            configurationProvider);
        _defaultDependencyProvider.ReflectionCallMethod(
            nameof(IDefaultDependencyProvider.RegisterGlobalContainerDefaults), new[] {typeof(object)},
            globalContainer);
        return globalContainer;
    }

    protected override void CreateTestRunner(object globalContainer, Assembly testAssembly)
    {
        globalContainer.ReflectionRegisterTypeAs(typeof(RuntimeBindingRegistryBuilderV301062Patch),
            typeof(IRuntimeBindingRegistryBuilder));

        var testRunnerManager = (TestRunnerManager) globalContainer.ReflectionResolve<ITestRunnerManager>();

        testRunnerManager.Initialize(testAssembly);

        testRunnerManager.CreateTestRunner(0);
    }

    protected override IBindingRegistry ResolveBindingRegistry(Assembly testAssembly, object globalContainer) =>
        globalContainer.ReflectionResolve<IBindingRegistry>();
}
