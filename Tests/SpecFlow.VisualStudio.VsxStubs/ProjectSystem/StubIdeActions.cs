using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using SpecFlow.VisualStudio.Diagnostics;
using SpecFlow.VisualStudio.Discovery;
using SpecFlow.VisualStudio.ProjectSystem;
using SpecFlow.VisualStudio.ProjectSystem.Actions;

namespace SpecFlow.VisualStudio.VsxStubs.ProjectSystem
{
    public class StubIdeActions : IIdeActions, IAsyncContextMenu
    {
        private readonly IIdeScope _ideScope;

        public SourceLocation LastNavigateToSourceLocation;
        public string LastShowContextMenuHeader;
        public List<ContextMenuItem> LastShowContextMenuItems;
        public QuestionDescription LastShowQuestion { get; set; }
        public string ClipboardText { get; private set; }

        public StubIdeActions(IIdeScope ideScope)
        {
            _ideScope = ideScope;
        }

        public bool NavigateTo(SourceLocation sourceLocation)
        {
            _ideScope.Logger.LogInfo("IDE Action performed");
            LastNavigateToSourceLocation = sourceLocation;
            return true;
        }

        public void ShowError(string description, Exception exception)
        {
            _ideScope.Logger.LogException(_ideScope.MonitoringService, exception, description);
        }

        public void ShowProblem(string description, string title = null)
        {
            _ideScope.Logger.LogWarning($"User Notification: {description}");
        }

        public void ShowQuestion(QuestionDescription questionDescription)
        {
            _ideScope.Logger.LogInfo($"User question: {questionDescription.Description}");
            LastShowQuestion = questionDescription;
        }

        public IAsyncContextMenu ShowContextMenu(string header, bool async, params ContextMenuItem[] contextMenuItems)
        {
            _ideScope.Logger.LogInfo("IDE Action performed");
            LastShowContextMenuHeader = header;
            LastShowContextMenuItems = contextMenuItems.ToList();
            IsComplete = !async;
            return this;
        }

        public void SetClipboardText(string text)
        {
            ClipboardText = text;
        }

        public void ResetMock()
        {
            LastNavigateToSourceLocation = null;
            LastShowContextMenuHeader = null;
            LastShowContextMenuItems = null;
        }

        public CancellationToken CancellationToken { get; } = new CancellationToken();
        public bool IsComplete { get; private set; }

        public void AddItems(params ContextMenuItem[] items)
        {
            LastShowContextMenuItems.AddRange(items);
        }

        public void Complete()
        {
            IsComplete = true;
        }
    }
}