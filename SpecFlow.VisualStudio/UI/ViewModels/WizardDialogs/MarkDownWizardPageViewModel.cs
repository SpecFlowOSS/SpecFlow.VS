using System;
using System.Linq;

namespace SpecFlow.VisualStudio.UI.ViewModels.WizardDialogs
{
    public class MarkDownWizardPageViewModel : WizardPageViewModel
    {
        public string Text { get; set; }

        public MarkDownWizardPageViewModel(string name) : base(name)
        {
        }
    }
}
