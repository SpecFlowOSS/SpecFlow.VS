using System;
using System.Globalization;
using System.IO;
using System.Linq;
using SpecFlow.VisualStudio.ProjectSystem;
using SpecFlow.VisualStudio.UI.ViewModels;
using SpecFlow.VisualStudio.Wizards.Infrastructure;

namespace SpecFlow.VisualStudio.Wizards
{
    public class SpecFlowProjectWizard : IDeveroomWizard
    {
        private readonly IDeveroomWindowManager _deveroomWindowManager;
        private string _projectDirectory;
        private string _solutionDirectory;

        public SpecFlowProjectWizard(IDeveroomWindowManager deveroomWindowManager)
        {
            _deveroomWindowManager = deveroomWindowManager;
        }

        public bool RunStarted(WizardRunParameters wizardRunParameters)
        {
            wizardRunParameters.IdeScope.Actions.ShowProblem("Hello from the wizard");

            try
            {
                _projectDirectory = wizardRunParameters.ReplacementsDictionary["$destinationdirectory$"];
                _solutionDirectory = wizardRunParameters.ReplacementsDictionary["$solutiondirectory$"];

                var viewModel = new AddNewSpecFlowProjectViewModel();
                var dialogResult = _deveroomWindowManager.ShowDialog(viewModel);
                if (dialogResult.HasValue && dialogResult.Value)
                {
                    // Add custom parameters.
                    wizardRunParameters.ReplacementsDictionary.Add("$dotnetframework$", viewModel.DotNetFramework);
                    wizardRunParameters.ReplacementsDictionary.Add("$unittestframework$", viewModel.UnitTestFramework);
                    wizardRunParameters.ReplacementsDictionary.Add("$fluentassertionsincluded$", viewModel.FluentAssertionsIncluded.ToString(CultureInfo.InvariantCulture));
                }
                else
                {
                    CancelProjectCreation();
                    return false;
                }
            }
            //catch (WizardBackoutException)
            //{
            //    throw;
            //}
            catch (Exception ex)
            {
                wizardRunParameters.IdeScope.Actions.ShowError("Error during project generation", ex);
                CancelProjectCreation();
                return false;
            }

            return true;
        }

        private void CancelProjectCreation()
        {
            Cleanup();

            // Cancel the project creation.
            //throw new WizardBackoutException();
        }

        private void Cleanup()
        {
            if (Directory.Exists(_projectDirectory))
            {
                Directory.Delete(_projectDirectory, true);
            }

            if (_projectDirectory != _solutionDirectory &&
                Directory.Exists(_solutionDirectory) &&
                !Directory.EnumerateFileSystemEntries(_solutionDirectory).Any())
            {
                Directory.Delete(_solutionDirectory);
            }
        }
    }
}