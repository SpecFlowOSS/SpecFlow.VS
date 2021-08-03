using System;
using System.ComponentModel.Composition;
using System.Linq;
using SpecFlow.VisualStudio.Editor.Commands.Infrastructure;
using SpecFlow.VisualStudio.Editor.Completions.Infrastructure;
using SpecFlow.VisualStudio.Monitoring;
using SpecFlow.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;

namespace SpecFlow.VisualStudio.Editor.Completions
{
    [Export(typeof(IDeveroomFeatureEditorCommand))]
    public class CompleteCommand : CompletionCommandBase, IDeveroomFeatureEditorCommand
    {
        [ImportingConstructor]
        public CompleteCommand(IIdeScope ideScope, IBufferTagAggregatorFactoryService aggregatorFactory, ICompletionBroker completionBroker, IMonitoringService monitoringService) : base(ideScope, aggregatorFactory, completionBroker, monitoringService)
        {
        }

        protected override bool ShouldStartSessionOnTyping(IWpfTextView textView, char ch)
        {
            if (char.IsWhiteSpace(ch))
                return false;

            if (ch == '|' || ch == '#' || ch == '*' || ch == '@') //TODO: get this from parser?
                return false;

            var caretBufferPosition = textView.Caret.Position.BufferPosition;

            var line = caretBufferPosition.GetContainingLine();
            if (caretBufferPosition == line.Start)
                return false; // we are at the beginning of a line (after an enter?)

            var linePrefixText = new SnapshotSpan(line.Start, caretBufferPosition.Subtract(1)).GetText();
            return linePrefixText.All(char.IsWhiteSpace); // start auto completion for the first typed in character in the line 
        }
    }
}
