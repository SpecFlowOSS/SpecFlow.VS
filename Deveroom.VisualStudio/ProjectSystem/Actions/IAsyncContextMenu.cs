using System.Threading;
using System.Windows.Controls;

namespace Deveroom.VisualStudio.ProjectSystem.Actions
{
    public interface IAsyncContextMenu
    {
        CancellationToken CancellationToken { get; }
        bool IsComplete { get; }
        void AddItems(params ContextMenuItem[] items);
        void Complete();
    }
}