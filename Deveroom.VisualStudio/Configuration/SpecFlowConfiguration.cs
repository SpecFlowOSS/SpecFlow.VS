using System;
using System.Linq;
using System.Text.RegularExpressions;
using Deveroom.VisualStudio.Common;
using Deveroom.VisualStudio.ProjectSystem.Settings;
using Equ;

namespace Deveroom.VisualStudio.Configuration
{
    public class SpecFlowConfiguration: MemberwiseEquatable<SpecFlowConfiguration>
    {
        public bool? IsSpecFlowProject { get; set; }

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

            if (Version != null && !Regex.IsMatch(Version, @"^(?:\.?[0-9]+){2,}(?:\-[\-a-z0-9]*)?$"))
                throw new DeveroomConfigurationException("'specFlow/version' was not in a correct format");
        }
    }
}
