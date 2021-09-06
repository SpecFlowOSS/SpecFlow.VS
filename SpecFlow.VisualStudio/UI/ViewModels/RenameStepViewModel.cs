using System;
using System.Linq;

namespace SpecFlow.VisualStudio.UI.ViewModels
{
    public class RenameStepViewModel
    {
        public string StepText {  get; set; }

#if DEBUG
        public static RenameStepViewModel DesignData = new ()
        {
            StepText = "I press add"
        };
#endif
    }
}
