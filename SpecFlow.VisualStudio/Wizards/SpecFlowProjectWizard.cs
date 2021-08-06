using System;
using System.Globalization;
using System.Linq;
using SpecFlow.VisualStudio.ProjectSystem;
using SpecFlow.VisualStudio.UI.ViewModels;
using SpecFlow.VisualStudio.Wizards.Infrastructure;

namespace SpecFlow.VisualStudio.Wizards
{
    public class SpecFlowProjectWizard : IDeveroomWizard
    {
        private readonly IDeveroomWindowManager _deveroomWindowManager;

        public SpecFlowProjectWizard(IDeveroomWindowManager deveroomWindowManager)
        {
            _deveroomWindowManager = deveroomWindowManager;
        }

        public bool RunStarted(WizardRunParameters wizardRunParameters)
        {
            var viewModel = new AddNewSpecFlowProjectViewModel();
            var dialogResult = _deveroomWindowManager.ShowDialog(viewModel);
            if (!dialogResult.HasValue || !dialogResult.Value)
            {
                return false;
            }

            // Add custom parameters.
            wizardRunParameters.ReplacementsDictionary.Add("$dotnetframework$", viewModel.DotNetFramework);
            wizardRunParameters.ReplacementsDictionary.Add("$unittestframework$", viewModel.UnitTestFramework);
            wizardRunParameters.ReplacementsDictionary.Add("$fluentassertionsincluded$",
                viewModel.FluentAssertionsIncluded.ToString(CultureInfo.InvariantCulture));

            return true;
        }

    }
}