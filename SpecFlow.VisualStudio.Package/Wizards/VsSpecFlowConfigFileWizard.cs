using SpecFlow.VisualStudio.Wizards.Infrastructure;
using EnvDTE;

namespace SpecFlow.VisualStudio.Wizards
{
    public class VsSpecFlowConfigFileWizard : VsProjectScopeWizard<SpecFlowConfigFileWizard>
    {
        protected override SpecFlowConfigFileWizard ResolveWizard(DTE dte)
        {
            return new SpecFlowConfigFileWizard();
        }
    }
}
