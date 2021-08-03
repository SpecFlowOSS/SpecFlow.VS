using System;
using System.ComponentModel.Composition;
using System.Linq;
using SpecFlow.VisualStudio.ProjectSystem;

namespace SpecFlow.VisualStudio.SpecFlowVsCompatibility
{
    [Export]
    public class SpecFlowVsCompatibilityService
    {
        private readonly IIdeScope _ideScope;
        private bool _compatibilityChecked = false;
        private bool _compatibilityAlertShown = false;

        [ImportingConstructor]
        public SpecFlowVsCompatibilityService(IIdeScope ideScope)
        {
            _ideScope = ideScope;
        }

        public void CheckCompatibilityOnce()
        {
            if (_compatibilityChecked)
                return;

            CheckCompatibility();
        }

        public void CheckCompatibility()
        {
            if (_compatibilityAlertShown)
                return;

            _compatibilityChecked = true;
            var specFlowVsDetected = AppDomain.CurrentDomain.GetAssemblies().Any(a =>
                a.FullName.StartsWith("TechTalk.SpecFlow.VsIntegration") ||
                a.FullName.StartsWith("TechTalk.SpecFlow.VisualStudio"));

            if (specFlowVsDetected && !_compatibilityAlertShown)
            {
                _compatibilityAlertShown = true;
                _ideScope.Actions.ShowProblem(
                    $"We detected that both the 'Deveroom for SpecFlow' and the 'SpecFlow for Visual Studio' extension has been installed for this Visual Studio instance.{Environment.NewLine}For the proper behavior you need to uninstall or disable one of these extensions in the 'Tools / Extensions and Updates...' dialog.");
            }
        }
    }
}
