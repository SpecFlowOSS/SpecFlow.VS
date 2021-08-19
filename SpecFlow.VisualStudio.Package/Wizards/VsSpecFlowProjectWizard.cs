using System;
using System.Linq;
using EnvDTE;
using SpecFlow.VisualStudio.Monitoring;
using SpecFlow.VisualStudio.ProjectSystem;
using SpecFlow.VisualStudio.Wizards.Infrastructure;

namespace SpecFlow.VisualStudio.Wizards
{
    public class VsSpecFlowProjectWizard : VsProjectScopeWizard<SpecFlowProjectWizard>
    {
        protected override SpecFlowProjectWizard ResolveWizard(DTE dte)
        {
            var windowManager = VsUtils.SafeResolveMefDependency<IDeveroomWindowManager>(dte);
            var monitoringService = VsUtils.SafeResolveMefDependency<IMonitoringService>(dte);
            return new SpecFlowProjectWizard(windowManager, monitoringService);
        }
    }
}
