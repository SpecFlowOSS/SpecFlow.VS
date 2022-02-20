using TechTalk.SpecFlow;
using TechTalk.SpecFlow.Bindings;
using TechTalk.SpecFlow.Configuration;
using TechTalk.SpecFlow.Infrastructure;

namespace SpecFlowConnector.SpecFlowProxies;

public class BindingRegistryFactoryBeforeV201000 : BindingRegistryFactoryBeforeV202000
{
    public BindingRegistryFactoryBeforeV201000(ILogger log) : base(log)
    {
    }

    protected override object CreateContainerBuilder(IDefaultDependencyProvider dependencyProvider)
    {
        var testRunContainerBuilderType = typeof(ITestRunner).Assembly
                                              .GetType("TechTalk.SpecFlow.Infrastructure.TestRunContainerBuilder", true)
                                          ?? throw new InvalidOperationException(
                                              "Couldn't find TechTalk.SpecFlow.Infrastructure.TestRunContainerBuilder");

        return Activator.CreateInstance(testRunContainerBuilderType, (IDefaultDependencyProvider) null!)
               ?? throw new TypeLoadException("Couldn't load TechTalk.SpecFlow.Infrastructure.TestRunContainerBuilder");
    }

    protected override object CreateObjectContainer(Assembly testAssembly, object containerBuilder,
        IRuntimeConfigurationProvider configurationProvider)
    {
        return containerBuilder.ReflectionCallMethod<object>(
            "CreateContainer", new[] {typeof(IRuntimeConfigurationProvider)}, new object[] {null!});
    }

    protected override IBindingRegistryAdapter AdaptBindingRegistry(IBindingRegistry bindingRegistry)
        => new BindingRegistryAdapterBeforeV20100(bindingRegistry);
}
