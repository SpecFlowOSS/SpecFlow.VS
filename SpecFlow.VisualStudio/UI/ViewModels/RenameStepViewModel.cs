using SpecFlow.VisualStudio.Discovery;
using SpecFlow.VisualStudio.ProjectSystem;

namespace SpecFlow.VisualStudio.UI.ViewModels
{
    public class RenameStepViewModel
    {
        public string StepText {  get; set; }
        public string OriginalStepText { get; }

        public IProjectScope SelectedStepDefinitionProject { get; }

        public ProjectStepDefinitionBinding SelectedStepDefinitionBinding { get; }

        public RenameStepViewModel(string stepText, IProjectScope selectedStepDefinitionProject, ProjectStepDefinitionBinding selectedStepDefinitionBinding)
        {
            StepText = stepText;
            SelectedStepDefinitionProject = selectedStepDefinitionProject;
            SelectedStepDefinitionBinding = selectedStepDefinitionBinding;
            OriginalStepText = stepText;
        }

#if DEBUG
        public static RenameStepViewModel DesignData = new("I press add", null, null);
#endif
    }
}
