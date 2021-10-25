using System;

namespace SpecFlow.VisualStudio.Configuration
{
    public class DeveroomConfiguration
    {
        public DateTimeOffset ConfigurationChangeTime { get; set; } = DateTimeOffset.MinValue;

        public string ConfigurationBaseFolder { get; set; }

        public SpecFlowConfiguration SpecFlow { get; set; } = new ();
        public TraceabilityConfiguration Traceability { get; set; } = new ();
        public EditorConfiguration Editor { get; set; } = new ();

        // old settings to be reviewed
        public ProcessorArchitectureSetting ProcessorArchitecture { get; set; } = ProcessorArchitectureSetting.AutoDetect;
        public bool DebugConnector { get; set; } = false;
        public string DefaultFeatureLanguage { get; set; } = "en-US";
        public string ConfiguredBindingCulture { get; set; } = null;
        public string BindingCulture => ConfiguredBindingCulture ?? DefaultFeatureLanguage;

        public DeveroomConfiguration()
        {
        }

        private void FixEmptyContainers()
        {
            SpecFlow ??= new ();
            Traceability ??= new ();
            Editor ??= new ();
        }

        public void CheckConfiguration()
        {
            FixEmptyContainers();

            SpecFlow.CheckConfiguration();
            Traceability.CheckConfiguration();
            Editor.CheckConfiguration();
        }

        #region Equality

        protected bool Equals(DeveroomConfiguration other)
        {
            return string.Equals(ConfigurationBaseFolder, other.ConfigurationBaseFolder) && Equals(SpecFlow, other.SpecFlow) && Equals(Traceability, other.Traceability) && Equals(Editor, other.Editor) && ProcessorArchitecture == other.ProcessorArchitecture && DebugConnector == other.DebugConnector && string.Equals(DefaultFeatureLanguage, other.DefaultFeatureLanguage) && string.Equals(ConfiguredBindingCulture, other.ConfiguredBindingCulture);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((DeveroomConfiguration) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (ConfigurationBaseFolder != null ? ConfigurationBaseFolder.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (SpecFlow != null ? SpecFlow.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Traceability != null ? Traceability.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Editor != null ? Editor.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (int) ProcessorArchitecture;
                hashCode = (hashCode * 397) ^ DebugConnector.GetHashCode();
                hashCode = (hashCode * 397) ^ (DefaultFeatureLanguage != null ? DefaultFeatureLanguage.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (ConfiguredBindingCulture != null ? ConfiguredBindingCulture.GetHashCode() : 0);
                return hashCode;
            }
        }

        #endregion
    }
}
