using System;
using System.Linq;

namespace SpecFlow.VisualStudio.ProjectSystem.Settings;

[Flags]
public enum SpecFlowProjectTraits
{
    None = 0,
    MsBuildGeneration = 1,
    XUnitAdapter = 2,
    DesignTimeFeatureFileGeneration = 4,
    CucumberExpression = 8
}
