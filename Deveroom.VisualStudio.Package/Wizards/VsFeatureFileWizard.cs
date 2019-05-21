using Deveroom.VisualStudio.Wizards.Infrastructure;
using EnvDTE;

namespace Deveroom.VisualStudio.Wizards
{
    public class VsFeatureFileWizard : VsSimulatedItemAddProjectScopeWizard<FeatureFileWizard>
    {
        protected override FeatureFileWizard ResolveWizard(DTE dte)
        {
            return new FeatureFileWizard();
        }
    }
}
