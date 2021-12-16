using System;
using SpecFlow.VisualStudio.Wizards.Infrastructure;

namespace SpecFlow.VisualStudio.Wizards;

public class SpecFlowConfigFileWizard : IDeveroomWizard
{
    public bool RunStarted(WizardRunParameters wizardRunParameters)
    {
        var projectSettings = wizardRunParameters.ProjectScope.GetProjectSettings();

        wizardRunParameters.MonitoringService.MonitorCommandAddSpecFlowConfigFile(projectSettings);

        if (projectSettings.IsSpecFlowProject && projectSettings.SpecFlowVersion.Version < new Version(3, 6, 23))
            wizardRunParameters.ReplacementsDictionary[WizardRunParameters.CopyToOutputDirectoryKey] = "PreserveNewest";
        return true;
    }
}
