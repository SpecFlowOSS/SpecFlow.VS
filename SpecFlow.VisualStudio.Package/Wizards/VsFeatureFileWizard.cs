using SpecFlow.VisualStudio.Wizards.Infrastructure;

namespace SpecFlow.VisualStudio.Wizards;

public class VsFeatureFileWizard : VsSimulatedItemAddProjectScopeWizard<FeatureFileWizard>
{
    protected override FeatureFileWizard ResolveWizard(DTE dte) => new();
}
