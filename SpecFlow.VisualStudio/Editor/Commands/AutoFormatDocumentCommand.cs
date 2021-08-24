using System;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using SpecFlow.VisualStudio.Editor.Commands.Infrastructure;
using SpecFlow.VisualStudio.Editor.Services;
using SpecFlow.VisualStudio.Editor.Services.Formatting;
using SpecFlow.VisualStudio.Editor.Services.Parser;
using SpecFlow.VisualStudio.Monitoring;
using SpecFlow.VisualStudio.ProjectSystem;

namespace SpecFlow.VisualStudio.Editor.Commands
{
    [Export(typeof(IDeveroomFeatureEditorCommand))]
    public class AutoFormatDocumentCommand : DeveroomEditorCommandBase, IDeveroomFeatureEditorCommand
    {
        internal static readonly DeveroomEditorCommandTargetKey FormatDocumentKey =
                    new(VSConstants.VSStd2K, VSConstants.VSStd2KCmdID.FORMATDOCUMENT);
        internal static readonly DeveroomEditorCommandTargetKey FormatSelectionKey =
                    new(VSConstants.VSStd2K, VSConstants.VSStd2KCmdID.FORMATSELECTION);

        public override DeveroomEditorCommandTargetKey[] Targets => new[]
        {
            FormatDocumentKey,
            FormatSelectionKey
        };

        private readonly GherkinDocumentFormatter _gherkinDocumentFormatter;

        [ImportingConstructor]
        public AutoFormatDocumentCommand(IIdeScope ideScope, IBufferTagAggregatorFactoryService aggregatorFactory, IMonitoringService monitoringService, GherkinDocumentFormatter gherkinDocumentFormatter) : base(ideScope, aggregatorFactory, monitoringService)
        {
            _gherkinDocumentFormatter = gherkinDocumentFormatter;
        }

        public override bool PreExec(IWpfTextView textView, DeveroomEditorCommandTargetKey commandKey, IntPtr inArgs = default(IntPtr))
        {
            //MonitoringService.MonitorAutoFormatDocument();

            var documentTag = GetDeveroomTagForCaret(textView, DeveroomTagTypes.Document);
            if (!(documentTag?.Data is DeveroomGherkinDocument gherkinDocument))
                return false;

            var isSelectionFormatting = commandKey.Equals(FormatSelectionKey);
            var textSnapshot = textView.TextSnapshot;
            var caretLineNumber = textView.Caret.Position.BufferPosition.GetContainingLine().LineNumber;
            
            var startLine = 0;
            var endLine = textSnapshot.LineCount - 1;
            
            if (isSelectionFormatting)
            {
                //MonitoringService.MonitorAutoFormatSelection();

                var selectionSpan = GetSelectionSpan(textView);
                startLine = selectionSpan.Start.GetContainingLine().LineNumber;
                endLine = selectionSpan.End.GetContainingLine().LineNumber;
            }

            var formatSettings = LoadFormatSettings(textView.Options);

            var lines = new DocumentLinesEditBuffer(textSnapshot, startLine, endLine);
            if (lines.IsEmpty)
                return false;

            _gherkinDocumentFormatter.FormatGherkinDocument(gherkinDocument, lines, formatSettings);
            var changeSpan = lines.GetSnapshotSpan();
            var newLine = GetNewLine(textView);
            var replacementText = lines.GetModifiedText(newLine);

            if (changeSpan.GetText().Equals(replacementText)) // no change
                return false;

            using (var textEdit = textSnapshot.TextBuffer.CreateEdit())
            {
                textEdit.Replace(changeSpan, replacementText);
                textEdit.Apply();
            }

            textView.Caret.MoveTo(textView.TextSnapshot.GetLineFromLineNumber(caretLineNumber).End);

            return true;
        }

        private static GherkinFormatSettings LoadFormatSettings(IEditorOptions editorOptions)
        {
            var formatSettings = new GherkinFormatSettings();

            var convertTabsToSpaces = editorOptions.GetOptionValue(DefaultOptions.ConvertTabsToSpacesOptionId);
            var indentSize = editorOptions.GetOptionValue(DefaultOptions.IndentSizeOptionId);
            formatSettings.Indent = convertTabsToSpaces ? new string(' ', indentSize) : new string('\t', 1);

            return formatSettings;
        }
    }
}