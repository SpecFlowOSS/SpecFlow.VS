using TechTalk.SpecFlow;
using TechTalk.SpecFlow.Bindings.Discovery;

namespace SpecFlowConnector.SpecFlowProxies;

public class BindingRegistryFactoryBeforeV301062 : BindingRegistryFactoryBeforeV307013
{
    public BindingRegistryFactoryBeforeV301062(ILogger log) : base(log)
    {
    }

    protected override ITestRunner CreateTestRunner(IObjectContainer globalContainer, Assembly testAssembly)
    {
        globalContainer.ReflectionRegisterTypeAs(typeof(RuntimeBindingRegistryBuilderV301062Patch),
            typeof(IRuntimeBindingRegistryBuilder));
        return base.CreateTestRunner(globalContainer, testAssembly);
    }
}
