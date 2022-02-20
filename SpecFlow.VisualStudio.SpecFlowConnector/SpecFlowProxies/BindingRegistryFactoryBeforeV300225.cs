using TechTalk.SpecFlow;
using TechTalk.SpecFlow.Bindings;
using TechTalk.SpecFlow.Bindings.Discovery;
using TechTalk.SpecFlow.Configuration;
using TechTalk.SpecFlow.Infrastructure;

namespace SpecFlowConnector.SpecFlowProxies;

public class BindingRegistryFactoryBeforeV300225 : BindingRegistryFactoryBeforeV301062
{
    public BindingRegistryFactoryBeforeV300225(ILogger log) : base(log)
    {
    }

    protected override object CreateConfigurationLoader(Option<FileDetails> configFile)
        => new SpecFlow21ConfigurationLoader(configFile);

    protected override object CreateDependencyProvider(AssemblyLoadContext assemblyLoadContext)
        => new SpecFlowDependencyProviderBeforeV300213(assemblyLoadContext);

    protected override object CreateObjectContainer(
        Assembly testAssembly,
        ContainerBuilder containerBuilder,
        IRuntimeConfigurationProvider configurationProvider,
        IDefaultDependencyProvider dependencyProvider)
    {
        var globalContainer = containerBuilder.ReflectionCallMethod<object>(
            nameof(ContainerBuilder.CreateGlobalContainer),
            configurationProvider);
        dependencyProvider.ReflectionCallMethod(
            nameof(IDefaultDependencyProvider.RegisterGlobalContainerDefaults), new[] {typeof(object)},
            globalContainer);
        return globalContainer;
    }

    protected override object CreateTestRunner(object globalContainer, Assembly testAssembly)
    {
        globalContainer.ReflectionRegisterTypeAs(typeof(RuntimeBindingRegistryBuilderV301062Patch),
            typeof(IRuntimeBindingRegistryBuilder));

        var testRunnerManager = (TestRunnerManager) globalContainer.ReflectionResolve<ITestRunnerManager>();

        testRunnerManager.Initialize(testAssembly);

        return testRunnerManager.CreateTestRunner(0);
    }

    protected override object ResolveBindingRegistry(Assembly testAssembly, object globalContainer, object testRunner) =>
        globalContainer.ReflectionResolve<IBindingRegistry>();
}
