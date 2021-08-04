using System.Collections.Generic;

namespace SpecFlow.VisualStudio.Configuration
{
    public class SpecFlowConfigDeserializer : IConfigDeserializer<DeveroomConfiguration>
    {
        class SpecFlowJsonConfiguration
        {
            public DeveroomConfiguration Ide { get; set; }
            public SpecFlowJsonConfiguration SpecFlow { get; set; }
            public Dictionary<string, string> Language { get; set; }
            public Dictionary<string, string> BindingCulture { get; set; }
        }

        private readonly JsonNetConfigDeserializer<SpecFlowJsonConfiguration> _specFlowConfigDeserializer = new ();
        
        public void Populate(string jsonString, DeveroomConfiguration config)
        {
            var specFlowJsonConfiguration = new SpecFlowJsonConfiguration() { Ide = config };
            // need to support specflow V2 configuration: where specflow.json had a specflow root node
            specFlowJsonConfiguration.SpecFlow = specFlowJsonConfiguration;
            _specFlowConfigDeserializer.Populate(jsonString, specFlowJsonConfiguration);
            if (specFlowJsonConfiguration.Language != null && specFlowJsonConfiguration.Language.TryGetValue("feature", out var featureLanguage))
                config.DefaultFeatureLanguage = featureLanguage;
            if (specFlowJsonConfiguration.BindingCulture != null && specFlowJsonConfiguration.BindingCulture.TryGetValue("name", out var bindingCulture))
                config.ConfiguredBindingCulture = bindingCulture;
        }
        
    }
}