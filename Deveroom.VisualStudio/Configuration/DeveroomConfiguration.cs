using System;
using Equ;

namespace Deveroom.VisualStudio.Configuration
{
    public class DeveroomConfiguration: MemberwiseEquatable<DeveroomConfiguration>
    {
        [MemberwiseEqualityIgnore]
        public DateTime ConfigurationChangeTime { get; set; } = DateTime.MinValue;

        public string ConfigurationBaseFolder { get; set; }

        public SpecFlowConfiguration SpecFlow { get; set; } = new SpecFlowConfiguration();

        public ProcessorArchitectureSetting ProcessorArchitecture { get; set; } = ProcessorArchitectureSetting.UseSystem;
        public bool DebugConnector { get; set; } = false;
        public string DefaultFeatureLanguage { get; set; } = "en-US";
        public string ConfiguredBindingCulture { get; set; } = null;
        public string BindingCulture => ConfiguredBindingCulture ?? DefaultFeatureLanguage;

        public DeveroomConfiguration()
        {
        }

        private void FixEmptyContainers()
        {
            SpecFlow = SpecFlow ?? new SpecFlowConfiguration();
        }

        public void CheckConfiguration()
        {
            FixEmptyContainers();

            SpecFlow.CheckConfiguration();
        }

    }
}
