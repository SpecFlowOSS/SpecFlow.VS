using EnvDTE;
using SpecFlow.VisualStudio.Wizards.Infrastructure;

namespace SpecFlow.VisualStudio.Wizards;

public class VsSpecFlowConfigFileWizard : VsProjectScopeWizard<SpecFlowConfigFileWizard>
{
    protected override SpecFlowConfigFileWizard ResolveWizard(DTE dte) => new();
}
