using TechTalk.SpecFlow;
using TechTalk.SpecFlow.Bindings.Discovery;
using TechTalk.SpecFlow.Infrastructure;

namespace SpecFlowConnector.SpecFlowProxies;

public class BindingRegistryFactoryBeforeV301062 : BindingRegistryFactoryBeforeV307013
{
    public BindingRegistryFactoryBeforeV301062(ILogger log) : base(log)
    {
    }

    protected override object PrepareTestRunnerCreation(object globalContainer, AssemblyLoadContext assemblyLoadContext)
    {
        globalContainer.ReflectionRegisterTypeAs(
            typeof(RuntimeBindingRegistryBuilderV301062Patch),
            typeof(IRuntimeBindingRegistryBuilder));

        globalContainer.ReflectionRegisterTypeAs(
            typeof(BindingAssemblyContextLoader),
            typeof(IBindingAssemblyLoader));

        return globalContainer;
    }
}