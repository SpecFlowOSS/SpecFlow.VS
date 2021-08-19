using System;
using System.Globalization;
using System.Linq;
using SpecFlow.VisualStudio.Monitoring;
using SpecFlow.VisualStudio.ProjectSystem;
using SpecFlow.VisualStudio.UI.ViewModels;
using SpecFlow.VisualStudio.Wizards.Infrastructure;

namespace SpecFlow.VisualStudio.Wizards
{
    public class SpecFlowProjectWizard : IDeveroomWizard
    {
        private readonly IDeveroomWindowManager _deveroomWindowManager;
        private readonly IMonitoringService _monitoringService;

        public SpecFlowProjectWizard(IDeveroomWindowManager deveroomWindowManager, IMonitoringService monitoringService)
        {
            _deveroomWindowManager = deveroomWindowManager;
            _monitoringService = monitoringService;
        }

        public bool RunStarted(WizardRunParameters wizardRunParameters)
        {
            _monitoringService.MonitorProjectTemplateWizardStarted();

            var viewModel = new AddNewSpecFlowProjectViewModel();
            var dialogResult = _deveroomWindowManager.ShowDialog(viewModel);
            if (!dialogResult.HasValue || !dialogResult.Value)
            {
                return false;
            }

            _monitoringService.MonitorProjectTemplateWizardCompleted(viewModel.DotNetFramework, viewModel.UnitTestFramework, viewModel.FluentAssertionsIncluded);

            // Add custom parameters.
            wizardRunParameters.ReplacementsDictionary.Add("$dotnetframework$", viewModel.DotNetFramework);
            wizardRunParameters.ReplacementsDictionary.Add("$unittestframework$", viewModel.UnitTestFramework);
            wizardRunParameters.ReplacementsDictionary.Add("$fluentassertionsincluded$",
                viewModel.FluentAssertionsIncluded.ToString(CultureInfo.InvariantCulture));

            return true;
        }

    }
}