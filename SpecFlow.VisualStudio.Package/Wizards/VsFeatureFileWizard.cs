using SpecFlow.VisualStudio.Wizards.Infrastructure;
using EnvDTE;

namespace SpecFlow.VisualStudio.Wizards
{
    public class VsFeatureFileWizard : VsSimulatedItemAddProjectScopeWizard<FeatureFileWizard>
    {
        protected override FeatureFileWizard ResolveWizard(DTE dte)
        {
            return new FeatureFileWizard();
        }
    }
}
