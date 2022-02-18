using TechTalk.SpecFlow.Configuration;

namespace SpecFlowConnector.SpecFlowProxies;

public class BindingRegistryFactoryBeforeV202000 : BindingRegistryFactoryBeforeV300225
{
    public BindingRegistryFactoryBeforeV202000(ILogger log) : base(log)
    {
    }

    protected override object CreateConfigurationLoader(Option<FileDetails> configFile) => None.Value;

    protected override IRuntimeConfigurationProvider CreateConfigurationProvider(object configurationLoader) =>
        Activator.CreateInstance<DefaultRuntimeConfigurationProvider>();
}
