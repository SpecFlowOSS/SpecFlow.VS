using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using SpecFlow.VisualStudio.Diagnostics;
using SpecFlow.VisualStudio.Discovery;
using SpecFlow.VisualStudio.Editor.Commands.Infrastructure;
using SpecFlow.VisualStudio.Editor.Services;
using SpecFlow.VisualStudio.Monitoring;
using SpecFlow.VisualStudio.ProjectSystem;
using SpecFlow.VisualStudio.ProjectSystem.Actions;
using SpecFlow.VisualStudio.Snippets.Fallback;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;

namespace SpecFlow.VisualStudio.Editor.Commands
{
    [Export(typeof(IDeveroomFeatureEditorCommand))]
    public class GoToStepDefinitionCommand : DeveroomEditorCommandBase, IDeveroomFeatureEditorCommand
    {
        const string PopupHeader = "Go to step definitions";

        [ImportingConstructor]
        public GoToStepDefinitionCommand(IIdeScope ideScope, IBufferTagAggregatorFactoryService aggregatorFactory, IMonitoringService monitoringService) : base(ideScope, aggregatorFactory, monitoringService)
        {
        }

        public override DeveroomEditorCommandTargetKey[] Targets => new[]
        {
            new DeveroomEditorCommandTargetKey(VSConstants.GUID_VSStandardCommandSet97, VSConstants.VSStd97CmdID.GotoDefn)
        };

        public override bool PreExec(IWpfTextView textView, DeveroomEditorCommandTargetKey commandKey, IntPtr inArgs = default(IntPtr))
        {
            Logger.LogVerbose("Go To Step Definition");

            var textBuffer = textView.TextBuffer;

            var stepTag = GetDeveroomTagForCaret(textView, DeveroomTagTypes.StepBlock);
            if (stepTag == null)
                return false;

            var matchedStepTag = stepTag.ChildTags.FirstOrDefault(t => t.Type == DeveroomTagTypes.DefinedStep || t.Type == DeveroomTagTypes.UndefinedStep);
            if (matchedStepTag != null && 
                matchedStepTag.Data is MatchResult matchResult)
            {
                if (matchResult.HasSingleMatch)
                {
                    var matchResultItem = matchResult.Items.First();
                    PerformGoToDefinition(matchResultItem, textBuffer);
                }
                else
                {
                    Logger.LogVerbose($"Jump to list step: {matchResult}");
                    IdeScope.Actions.ShowSyncContextMenu(PopupHeader, matchResult.Items.Select(m => 
                        new ContextMenuItem(m.ToString(), _ => { PerformGoToDefinition(m, textBuffer); }, GetIcon(m))
                    ).ToArray());
                }
            }

            return true;
        }

        private void PerformGoToDefinition(MatchResultItem match, ITextBuffer textBuffer)
        {
            MonitoringService.MonitorCommandGoToStepDefinition(match.Type == MatchResultType.Undefined);
            switch (match.Type)
            {
                case MatchResultType.Undefined:
                    PerformOfferCopySnippet(match, textBuffer);
                    break;
                case MatchResultType.Defined:
                case MatchResultType.Ambiguous:
                    PerformJump(match);
                    break;
            }
        }

        private void PerformJump(MatchResultItem match)
        {
            var sourceLocation = match.MatchedStepDefinition.Implementation.SourceLocation;
            if (sourceLocation == null)
            {
                Logger.LogWarning($"Cannot jump to {match}: no source location");
                IdeScope.Actions.ShowProblem("Unable to jump to the step definition. No source location detected.");
                return;
            }

            Logger.LogInfo($"Jumping to {match} at {sourceLocation}");
            if (!IdeScope.Actions.NavigateTo(sourceLocation))
            {
                Logger.LogWarning($"Cannot jump to {match}: invalid source file or position. Try to build the project to refresh positions.");
                IdeScope.Actions.ShowProblem($"Unable to jump to the step definition. Invalid source file or file position.{Environment.NewLine}{sourceLocation}");
            }
        }

        private void PerformOfferCopySnippet(MatchResultItem match, ITextBuffer textBuffer)
        {
            Debug.Assert(match.UndefinedStep != null);
            var snippetService = GetProjectScope(textBuffer).GetSnippetService();
            if (snippetService == null)
                return;

            const string indent = "    ";
            string newLine = Environment.NewLine;

            var snippet = snippetService.GetStepDefinitionSkeletonSnippet(match.UndefinedStep, snippetService.DefaultExpressionStyle, indent, newLine);

            IdeScope.Actions.ShowQuestion(new QuestionDescription(PopupHeader, $"The step is undefined. Do you want to copy a step definition skeleton snippet to the clipboard?{Environment.NewLine}{Environment.NewLine}{snippet}", _ => PerformCopySnippet(snippet.Indent(indent + indent))));
        }

        private void PerformCopySnippet(string snippet)
        {
            Logger.LogVerbose($"Copy to clipboard: {snippet}");
            IdeScope.Actions.SetClipboardText(snippet);
        }

        private IProjectScope GetProjectScope(ITextBuffer textBuffer)
        {
            return IdeScope.GetProject(textBuffer);
        }

        private string GetIcon(MatchResultItem matchResult)
        {
            switch (matchResult.Type)
            {
                case MatchResultType.Defined:
                    if (matchResult.HasErrors)
                        return "StepDefinitionsDefinedInvalid";
                    return "StepDefinitionsDefined";
                case MatchResultType.Ambiguous:
                    return "StepDefinitionsAmbiguous";
                case MatchResultType.Undefined:
                    return "StepDefinitionsUndefined";
            }
            return null;
        }
    }
}
