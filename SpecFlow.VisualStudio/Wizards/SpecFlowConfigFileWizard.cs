using System;
using SpecFlow.VisualStudio.ProjectSystem.Settings;
using SpecFlow.VisualStudio.Wizards.Infrastructure;

namespace SpecFlow.VisualStudio.Wizards
{
    public class SpecFlowConfigFileWizard : IDeveroomWizard
    {
        public bool RunStarted(WizardRunParameters wizardRunParameters)
        {
            wizardRunParameters.MonitoringService.MonitorCommandAddSpecFlowConfigFile();

            var projectSettings = wizardRunParameters.ProjectScope.GetProjectSettings();

            if (projectSettings.IsSpecFlowProject && projectSettings.SpecFlowVersion.Version < new Version(3, 6, 23))
            {
                wizardRunParameters.ReplacementsDictionary[WizardRunParameters.CopyToOutputDirectoryKey] = "PreserveNewest";
            }
            return true;
        }
    }
}