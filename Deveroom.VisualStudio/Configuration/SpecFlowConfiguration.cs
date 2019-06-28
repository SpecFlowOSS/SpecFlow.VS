using System;
using System.Linq;
using Deveroom.VisualStudio.ProjectSystem.Settings;

namespace Deveroom.VisualStudio.Configuration
{
    public class SpecFlowConfiguration
    {
        public bool IsSpecFlowProject { get; set; }

        public string Version { get; set; }
        public string GeneratorFolder { get; set; }
        public string ConfigFilePath { get; set; }
        public SpecFlowProjectTraits[] Traits { get; set; } = new SpecFlowProjectTraits[0];

        private void FixEmptyContainers()
        {
            Traits = Traits ?? new SpecFlowProjectTraits[0];
        }

        public void CheckConfiguration()
        {
            FixEmptyContainers();
        }

        #region Equality

        protected bool Equals(SpecFlowConfiguration other)
        {
            return IsSpecFlowProject == other.IsSpecFlowProject && string.Equals(Version, other.Version) && string.Equals(GeneratorFolder, other.GeneratorFolder) && string.Equals(ConfigFilePath, other.ConfigFilePath) && Equals(Traits, other.Traits);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((SpecFlowConfiguration) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = IsSpecFlowProject.GetHashCode();
                hashCode = (hashCode * 397) ^ (Version != null ? Version.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (GeneratorFolder != null ? GeneratorFolder.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (ConfigFilePath != null ? ConfigFilePath.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Traits != null ? Traits.GetHashCode() : 0);
                return hashCode;
            }
        }

        #endregion
    }
}
