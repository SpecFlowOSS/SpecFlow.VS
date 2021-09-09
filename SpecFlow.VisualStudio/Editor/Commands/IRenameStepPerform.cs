using Microsoft.VisualStudio.Text;
using SpecFlow.VisualStudio.UI.ViewModels;

namespace SpecFlow.VisualStudio.Editor.Commands
{
    internal interface IRenameStepPerform
    {
        void PerformRenameStep(RenameStepViewModel viewModel, ITextBuffer textBufferOfStepDefinitionClass);
    }
}