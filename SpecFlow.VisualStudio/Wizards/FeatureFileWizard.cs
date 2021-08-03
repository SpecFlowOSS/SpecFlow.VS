using SpecFlow.VisualStudio.ProjectSystem;
using SpecFlow.VisualStudio.ProjectSystem.Settings;
using SpecFlow.VisualStudio.Wizards.Infrastructure;

namespace SpecFlow.VisualStudio.Wizards
{
    public class FeatureFileWizard : IDeveroomWizard
    {
        public bool RunStarted(WizardRunParameters wizardRunParameters)
        {
            var projectSettings = wizardRunParameters.ProjectScope.GetProjectSettings();

            wizardRunParameters.MonitoringService.MonitorCommandAddFeatureFile(projectSettings);

            if (projectSettings.IsSpecFlowProject)
            {
                if (projectSettings.DesignTimeFeatureFileGenerationEnabled)
                    wizardRunParameters.ReplacementsDictionary[WizardRunParameters.CustomToolSettingKey] = "SpecFlowSingleFileGenerator";
                else if (!projectSettings.HasDesignTimeGenerationReplacement)
                {
                    wizardRunParameters.ProjectScope.IdeScope.Actions.ShowProblem($"In order to be able to run the SpecFlow scenarios as tests, you need to install the '{SpecFlowPackageDetector.SpecFlowToolsMsBuildGenerationPackageName}' NuGet package to the project.");
                }

                if (projectSettings.SpecFlowProjectTraits.HasFlag(SpecFlowProjectTraits.XUnitAdapter))
                {
                    wizardRunParameters.ReplacementsDictionary[WizardRunParameters.BuildActionKey] = "SpecFlowEmbeddedFeature";
                }
            }
            return true;
        }
    }
}