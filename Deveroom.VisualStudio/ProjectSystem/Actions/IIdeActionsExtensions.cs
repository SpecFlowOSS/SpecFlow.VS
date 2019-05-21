using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Deveroom.VisualStudio.ProjectSystem.Actions
{
    public static class IdeActionsExtensions
    {
        public static void ShowSyncContextMenu(this IIdeActions ideActions, string header, params ContextMenuItem[] contextMenuItems)
        {
            ideActions.ShowContextMenu(header, false, contextMenuItems);
        }

        public static IAsyncContextMenu ShowAsyncContextMenu(this IIdeActions ideActions, string header, params ContextMenuItem[] contextMenuItems)
        {
            return ideActions.ShowContextMenu(header, true, contextMenuItems);
        }
    }
}
