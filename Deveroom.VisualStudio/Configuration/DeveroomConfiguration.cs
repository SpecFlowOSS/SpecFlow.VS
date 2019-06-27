using System;

namespace Deveroom.VisualStudio.Configuration
{
    public class DeveroomConfiguration
    {
        public DateTime ConfigurationChangeTime { get; set; } = DateTime.MinValue;

        public string ConfigurationBaseFolder { get; set; }

        public ProcessorArchitectureSetting ProcessorArchitecture { get; set; } = ProcessorArchitectureSetting.UseSystem;
        public bool DebugConnector { get; set; } = false;
        public string DefaultFeatureLanguage { get; set; } = "en-US";
        public string ConfiguredBindingCulture { get; set; } = null;
        public string BindingCulture => ConfiguredBindingCulture ?? DefaultFeatureLanguage;

        public DeveroomConfiguration()
        {
        }

        #region Equality

        protected bool Equals(DeveroomConfiguration other)
        {
            return ProcessorArchitecture == other.ProcessorArchitecture && DebugConnector == other.DebugConnector && string.Equals(DefaultFeatureLanguage, other.DefaultFeatureLanguage) && string.Equals(ConfiguredBindingCulture, other.ConfiguredBindingCulture);
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
                var hashCode = (int) ProcessorArchitecture;
                hashCode = (hashCode * 397) ^ DebugConnector.GetHashCode();
                hashCode = (hashCode * 397) ^ (DefaultFeatureLanguage != null ? DefaultFeatureLanguage.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (ConfiguredBindingCulture != null ? ConfiguredBindingCulture.GetHashCode() : 0);
                return hashCode;
            }
        }

        #endregion
    }
}
