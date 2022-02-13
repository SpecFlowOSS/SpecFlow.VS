using TechTalk.SpecFlow.Bindings;

namespace SpecFlowConnector.SpecFlowProxies;

public record StepDefinitionBindingAdapter(IStepDefinitionBinding Adaptee)
{
    public string StepDefinitionType => Adaptee.StepDefinitionType.ToString();
    public Option<Regex> Regex => Adaptee.Regex;
    public BindingMethodAdapter Method { get; } = new BindingMethodAdapter(Adaptee.Method);
    public bool IsScoped => Adaptee.IsScoped;
    public Option<string> BindingScopeTag => Adaptee.BindingScope.Tag;
    public string BindingScopeFeatureTitle => Adaptee.BindingScope.FeatureTitle;
    public string BindingScopeScenarioTitle => Adaptee.BindingScope.ScenarioTitle;
    public virtual Option<T> GetProperty<T>(string parameterName) => None<T>.Value;
}
