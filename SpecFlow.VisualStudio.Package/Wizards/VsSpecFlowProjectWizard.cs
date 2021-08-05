using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EnvDTE;
using SpecFlow.VisualStudio.ProjectSystem;
using SpecFlow.VisualStudio.Wizards.Infrastructure;

namespace SpecFlow.VisualStudio.Wizards
{
    public class VsSpecFlowProjectWizard : VsProjectScopeWizard<SpecFlowProjectWizard>
    {
        protected override SpecFlowProjectWizard ResolveWizard(DTE dte)
        {
            var windowManager = VsUtils.SafeResolveMefDependency<IDeveroomWindowManager>(dte);
            return new SpecFlowProjectWizard(windowManager);
        }
    }
}
