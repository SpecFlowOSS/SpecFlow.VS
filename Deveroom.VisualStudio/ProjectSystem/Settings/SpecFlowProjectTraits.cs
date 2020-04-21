using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Deveroom.VisualStudio.ProjectSystem.Settings
{
    [Flags]
    public enum SpecFlowProjectTraits
    {
        None = 0,
        MsBuildGeneration = 1,
        XUnitAdapter = 2,
        DesignTimeFeatureFileGeneration = 4,
        CucumberExpression = 8,
    }
}
