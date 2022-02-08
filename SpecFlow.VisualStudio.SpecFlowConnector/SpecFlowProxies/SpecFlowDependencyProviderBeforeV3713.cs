using BoDi;
using TechTalk.SpecFlow.Bindings;

namespace SpecFlowConnector.SpecFlowProxies;

public class SpecFlowDependencyProviderBeforeV3713 : SpecFlowDependencyProviderBeforeV3922
{
    public SpecFlowDependencyProviderBeforeV3713(AssemblyLoadContext loadContext) : base(loadContext)
    {
    }

    protected override void RegisterBindingInvoker(ObjectContainer container)
    {
        container.ReflectionRegisterTypeAs<NullBindingInvoker, IBindingInvoker>();
    }
}
