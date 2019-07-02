using System;
using System.Linq;

namespace Deveroom.VisualStudio.ProjectSystem.Settings
{
    public class SpecFlowSettings
    {
        public NuGetVersion Version { get; set; }
        public SpecFlowProjectTraits Traits { get; set; }
        public string GeneratorFolder { get; set; }
        public string ConfigFilePath { get; set; }

        public SpecFlowSettings()
        {
            Traits = SpecFlowProjectTraits.None;
        }

        public SpecFlowSettings(NuGetVersion version, SpecFlowProjectTraits traits, string generatorFolder, string configFilePath)
        {
            Version = version;
            Traits = traits;
            GeneratorFolder = generatorFolder;
            ConfigFilePath = configFilePath;
        }
    }
}
