using TechTalk.SpecFlow;
using TechTalk.SpecFlow.Bindings;
using TechTalk.SpecFlow.Bindings.Discovery;
using TechTalk.SpecFlow.Configuration;
using TechTalk.SpecFlow.Infrastructure;

namespace SpecFlowConnector.SpecFlowProxies;

public class BindingRegistryFactoryBeforeV200000 : BindingRegistryFactoryBeforeV201000
{
    public BindingRegistryFactoryBeforeV200000(ILogger log) : base(log)
    {
    }

    protected override object CreateDependencyProvider(AssemblyLoadContext assemblyLoadContext) => None.Value;

    protected override object CreateContainerBuilder(object dependencyProvider) => None.Value;

    protected override object CreateObjectContainer(
        Assembly testAssembly,
        object containerBuilder,
        IRuntimeConfigurationProvider configurationProvider,
        object dependencyProvider)
    {
        var testRunContainerBuilderType
            = typeof(ITestRunner)
                  .Assembly.GetType("TechTalk.SpecFlow.Infrastructure.TestRunContainerBuilder", true)
              ?? throw new InvalidOperationException(
                  "Couldn't find TechTalk.SpecFlow.Infrastructure.TestRunContainerBuilder");
        ;
        var container = testRunContainerBuilderType
            .ReflectionCallStaticMethod<object>("CreateContainer",
                new[] {typeof(IRuntimeConfigurationProvider)}, new object[] {null!});
        return container;
    }

    protected override object CreateTestRunner(object globalContainer, Assembly testAssembly)
    {
        globalContainer.ReflectionRegisterTypeAs(typeof(RuntimeBindingRegistryBuilderV301062Patch),
            typeof(IRuntimeBindingRegistryBuilder));
        return globalContainer
            .ReflectionResolve<ITestRunnerFactory>()
            .Create(testAssembly);
    }

    protected override object ResolveBindingRegistry(Assembly testAssembly, object globalContainer, object testRunner)
    {
        var testExecutionEngine = testRunner.ReflectionGetField<ITestExecutionEngine>("executionEngine");
        return testExecutionEngine.ReflectionGetField<IBindingRegistry>("bindingRegistry");
    }
}
