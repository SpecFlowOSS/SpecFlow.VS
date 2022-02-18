using TechTalk.SpecFlow.Bindings.Discovery;

namespace SpecFlowConnector.SpecFlowProxies;

public class BindingRegistryFactoryBeforeV301062 : BindingRegistryFactoryBeforeV307013
{
    public BindingRegistryFactoryBeforeV301062(ILogger log) : base(log)
    {
    }

    protected override void CreateTestRunner(IObjectContainer globalContainer, Assembly testAssembly)
    {
        globalContainer.ReflectionRegisterTypeAs(typeof(RuntimeBindingRegistryBuilderV301062Patch),
            typeof(IRuntimeBindingRegistryBuilder));
        base.CreateTestRunner(globalContainer, testAssembly);
    }
}
