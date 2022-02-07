using TechTalk.SpecFlow.Bindings.Reflection;

namespace SpecFlowConnector.SpecFlowProxies;

public record BindingMethodAdapter(IBindingMethod Adaptee)
{
    public override string ToString() => Adaptee.ToString();
    public IEnumerable<string> ParameterTypeNames => Adaptee.Parameters.Select(p=>p.Type.FullName);
    public Option<MethodInfo> MethodInfo => (Adaptee as RuntimeBindingMethod).MethodInfo;
}
