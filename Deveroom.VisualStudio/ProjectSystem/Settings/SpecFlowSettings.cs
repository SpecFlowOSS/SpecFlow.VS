using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Deveroom.VisualStudio.ProjectSystem.Settings
{
    public class SpecFlowSettings
    {
        public NuGetVersion Version { get; }
        public SpecFlowProjectTraits Traits { get; }
        public string SpecFlowGeneratorFolder { get; }

        public SpecFlowSettings(NuGetVersion version, SpecFlowProjectTraits traits, string specFlowGeneratorFolder)
        {
            Version = version;
            Traits = traits;
            SpecFlowGeneratorFolder = specFlowGeneratorFolder;
        }
    }
}
