namespace SpecFlow.VisualStudio.UI.ViewModels
{
    public class RenameStepViewModel
    {
        public string StepText {  get; set; }
        public string OriginalStepText { get; }

        public RenameStepViewModel(string stepText)
        {
            StepText = stepText;
            OriginalStepText = stepText;
        }

#if DEBUG
        public static RenameStepViewModel DesignData = new("I press add");
#endif
    }
}
