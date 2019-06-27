using System;
using System.Linq;

namespace Deveroom.VisualStudio.ProjectSystem.Settings
{
    public class SpecFlowSettings
    {
        public NuGetVersion Version { get; }
        public SpecFlowProjectTraits Traits { get; }
        public string GeneratorFolder { get; }
        public string ConfigFilePath { get; }

        public SpecFlowSettings(NuGetVersion version, SpecFlowProjectTraits traits, string generatorFolder, string configFilePath)
        {
            Version = version;
            Traits = traits;
            GeneratorFolder = generatorFolder;
            ConfigFilePath = configFilePath;
        }
    }
}
