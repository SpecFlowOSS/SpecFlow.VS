using System;

namespace Deveroom.VisualStudio.ProjectSystem.Actions
{
    public class ContextMenuItem
    {
        public string Label { get; }
        public Action<ContextMenuItem> Command { get; }
        public string Icon { get; set; }

        public ContextMenuItem(string label, Action<ContextMenuItem> command = null, string icon = null)
        {
            Label = label;
            Command = command;
            Icon = icon;
        }
    }
}