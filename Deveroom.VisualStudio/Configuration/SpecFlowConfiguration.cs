using System;
using System.Linq;
using Deveroom.VisualStudio.ProjectSystem.Settings;
using Equ;

namespace Deveroom.VisualStudio.Configuration
{
    public class SpecFlowConfiguration: MemberwiseEquatable<SpecFlowConfiguration>
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
    }
}
