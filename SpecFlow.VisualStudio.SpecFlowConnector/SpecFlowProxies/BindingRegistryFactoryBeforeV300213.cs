using TechTalk.SpecFlow;
using TechTalk.SpecFlow.Bindings;
using TechTalk.SpecFlow.Configuration;
using TechTalk.SpecFlow.Infrastructure;

namespace SpecFlowConnector.SpecFlowProxies;

public class BindingRegistryFactoryBeforeV300213 : BindingRegistryFactoryBeforeV307013
{
    public BindingRegistryFactoryBeforeV300213(IFileSystem fileSystem) : base(fileSystem)
    {
    }

    protected override object CreateObjectContainer(
        Assembly testAssembly,
        ContainerBuilder containerBuilder,
        IRuntimeConfigurationProvider configurationProvider)
    {
        return containerBuilder.ReflectionCallMethod<object>(nameof(ContainerBuilder.CreateGlobalContainer),
            configurationProvider);
    }

    protected override void CreateTestRunner(object globalContainer, Assembly testAssembly)
    {
        var testRunnerManager = (TestRunnerManager)globalContainer.ReflectionResolve<ITestRunnerManager>();

        testRunnerManager.Initialize(testAssembly);
        testRunnerManager.CreateTestRunner(0);
    }

    protected override IBindingRegistry ResolveBindingRegistry(Assembly testAssembly, object globalContainer)
    {
        return globalContainer.ReflectionResolve<IBindingRegistry>();
    }
}
