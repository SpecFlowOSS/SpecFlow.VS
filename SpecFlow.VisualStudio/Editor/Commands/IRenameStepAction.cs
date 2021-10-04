using System.Threading.Tasks;
using Microsoft.VisualStudio.Text;
using SpecFlow.VisualStudio.UI.ViewModels;

namespace SpecFlow.VisualStudio.Editor.Commands
{
    internal interface IRenameStepAction
    {
        Task PerformRenameStep(RenameStepCommandContext ctx);
    }
}