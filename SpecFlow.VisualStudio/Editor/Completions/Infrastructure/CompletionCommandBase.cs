using System;
using System.Linq;
using SpecFlow.VisualStudio.Editor.Commands.Infrastructure;
using SpecFlow.VisualStudio.Monitoring;
using SpecFlow.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;

namespace SpecFlow.VisualStudio.Editor.Completions.Infrastructure
{
    public abstract class CompletionCommandBase : DeveroomEditorTypeCharCommandBase
    {
        private static readonly DeveroomEditorCommandTargetKey CompleteWordCommand = new DeveroomEditorCommandTargetKey(VSConstants.VSStd2K, VSConstants.VSStd2KCmdID.COMPLETEWORD);
        private static readonly DeveroomEditorCommandTargetKey AutoCompleteCommand = new DeveroomEditorCommandTargetKey(VSConstants.VSStd2K, VSConstants.VSStd2KCmdID.AUTOCOMPLETE);
        internal static readonly DeveroomEditorCommandTargetKey ReturnCommand = new DeveroomEditorCommandTargetKey(VSConstants.VSStd2K, VSConstants.VSStd2KCmdID.RETURN);
        private static readonly DeveroomEditorCommandTargetKey TabCommand = new DeveroomEditorCommandTargetKey(VSConstants.VSStd2K, VSConstants.VSStd2KCmdID.TAB);
        private static readonly DeveroomEditorCommandTargetKey CancelCommand = new DeveroomEditorCommandTargetKey(VSConstants.VSStd2K, VSConstants.VSStd2KCmdID.CANCEL);
        private static readonly DeveroomEditorCommandTargetKey BackspaceCommand = new DeveroomEditorCommandTargetKey(VSConstants.VSStd2K, VSConstants.VSStd2KCmdID.BACKSPACE);

        private static readonly DeveroomEditorCommandTargetKey[] AutoCompleteCommands = new[]
        {
            CompleteWordCommand, AutoCompleteCommand
        };

        private DeveroomEditorCommandTargetKey TypeCharCommand => base.Target;

        public override DeveroomEditorCommandTargetKey[] Targets => new[]
        {
            CompleteWordCommand,
            AutoCompleteCommand,
            ReturnCommand,
            TabCommand,
            CancelCommand,
            BackspaceCommand,
            base.Target,
        };

        protected readonly ICompletionBroker _completionBroker;

        protected CompletionCommandBase(IIdeScope ideScope, IBufferTagAggregatorFactoryService aggregatorFactory, ICompletionBroker completionBroker, IMonitoringService monitoringService) : base(ideScope, aggregatorFactory, monitoringService)
        {
            _completionBroker = completionBroker;
        }

        protected abstract bool ShouldStartSessionOnTyping(IWpfTextView textView, char? ch, bool isSessionActive);

        protected virtual CompletionSessionManager CreateCompletionSessionManager(IWpfTextView textView)
        {
            return new CompletionSessionManager(textView, _completionBroker);
        }

        public override bool PreExec(IWpfTextView textView, DeveroomEditorCommandTargetKey commandKey, IntPtr inArgs = default(IntPtr))
        {
            var sessionManager = GetCompletionSessionManager(textView);

            if (AutoCompleteCommands.Contains(commandKey))
                return sessionManager.TriggerCompletion();

            if (commandKey.Equals(ReturnCommand))
                return sessionManager.Complete(false);

            if (commandKey.Equals(TabCommand))
                return sessionManager.Complete(true);

            if (commandKey.Equals(CancelCommand))
                return sessionManager.Cancel();

            return false;
        }

        public override bool PostExec(IWpfTextView textView, DeveroomEditorCommandTargetKey commandKey, IntPtr inArgs = default(IntPtr))
        {
            var sessionManager = GetCompletionSessionManager(textView);

            if (commandKey.Equals(TypeCharCommand))
                return base.PostExec(textView, commandKey, inArgs);

            if (commandKey.Equals(BackspaceCommand))
                return sessionManager.Filter();

            if (!sessionManager.IsActive && 
                (commandKey.Equals(ReturnCommand) || commandKey.Equals(TabCommand) || AutoCompleteCommands.Contains(commandKey)) &&
                ShouldStartSessionOnTyping(textView, null, sessionManager.IsActive))
                return sessionManager.TriggerCompletion();

            return false;
        }

        protected internal override bool PostExec(IWpfTextView textView, char ch)
        {
            var sessionManager = GetCompletionSessionManager(textView);

            if (ShouldStartSessionOnTyping(textView, ch, sessionManager.IsActive))
            {
                if (sessionManager.IsActive)
                    sessionManager.Cancel();
                return sessionManager.TriggerCompletion();
            }

            if (sessionManager.IsActive)
                return sessionManager.Filter();

            return false;
        }

        private CompletionSessionManager GetCompletionSessionManager(IWpfTextView textView)
        {
            return textView.Properties.GetOrCreateSingletonProperty(() => CreateCompletionSessionManager(textView));
        }
    }
}
