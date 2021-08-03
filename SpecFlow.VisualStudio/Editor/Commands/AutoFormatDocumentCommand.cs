using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Gherkin.Ast;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using SpecFlow.VisualStudio.Editor.Commands.Infrastructure;
using SpecFlow.VisualStudio.Editor.Services;
using SpecFlow.VisualStudio.Editor.Services.Parser;
using SpecFlow.VisualStudio.Monitoring;
using SpecFlow.VisualStudio.ProjectSystem;

namespace SpecFlow.VisualStudio.Editor.Commands
{
    [Export(typeof(IDeveroomFeatureEditorCommand))]
    public class AutoFormatDocumentCommand : DeveroomEditorCommandBase, IDeveroomFeatureEditorCommand
    {
        private const int PADDING_LENGHT = 1;
        private const int PIPE_LENGHT = 1;

        public override DeveroomEditorCommandTargetKey[] Targets => new[]
        {
            new DeveroomEditorCommandTargetKey(VSConstants.VSStd2K, VSConstants.VSStd2KCmdID.FORMATDOCUMENT)
        };

        [ImportingConstructor]
        public AutoFormatDocumentCommand(IIdeScope ideScope, IBufferTagAggregatorFactoryService aggregatorFactory, IMonitoringService monitoringService) : base(ideScope, aggregatorFactory, monitoringService)
        {
        }

        public override bool PreExec(IWpfTextView textView, DeveroomEditorCommandTargetKey commandKey, IntPtr inArgs = default(IntPtr))
        {
            //MonitoringService.MonitorCommandCommentUncomment();

            var documentTag = GetDeveroomTagForCaret(textView, DeveroomTagTypes.Document);
            if (!(documentTag?.Data is DeveroomGherkinDocument gherkinDocument))
                return false;

            var selectionSpan = GetSelectionSpan(textView);
            //var lines = GetSpanFullLines(selectionSpan).ToArray();
            var formattingSpan = new SnapshotSpan(textView.TextBuffer.CurrentSnapshot, 0, textView.TextBuffer.CurrentSnapshot.Length);
            var lines = GetSpanFullLines(formattingSpan).Select(l => l.GetText()).ToArray();



            Debug.Assert(lines.Length > 0);


            string indent = "    ";
            var caretLineNumber = textView.Caret.Position.BufferPosition.GetContainingLine().LineNumber;
            using (var textEdit = selectionSpan.Snapshot.TextBuffer.CreateEdit())
            {
                textEdit.Replace(formattingSpan, FormatDocument(lines, GetNewLine(textView), gherkinDocument, indent, formattingSpan.Snapshot));
                textEdit.Apply();
            }
            
            textView.Caret.MoveTo(textView.TextSnapshot.GetLineFromLineNumber(caretLineNumber).End);

            return true;
        }

        private string FormatDocument(string[] lines, string newLine,
            DeveroomGherkinDocument gherkinDocument, string indent, ITextSnapshot textSnapshot)
        {
            if (gherkinDocument.Feature != null)
            {
                SetLine(lines, gherkinDocument.Feature, $"{gherkinDocument.Feature.Keyword}: {gherkinDocument.Feature.Name}");

                foreach (var featureChild in gherkinDocument.Feature.Children)
                {
                    if (featureChild is Scenario scenario)
                    {
                        SetLine(lines, scenario, $"{scenario.Keyword}: {scenario.Name}");
                    }
                    if (featureChild is IHasSteps hasSteps)
                    {
                        foreach (var step in hasSteps.Steps)
                        {
                            SetLine(lines, step, $"{indent}{step.Keyword}{step.Text}");
                            if (step.Argument is DataTable dataTable)
                            {
                                FormatTable(lines, dataTable, indent + indent, newLine);
                            }
                        }

                        
                    }

                    //todo: handle ScenarioOutline, Rule, Background, etc.
                    
                }
            }

            
            return string.Join(newLine, lines);
        }

        private void FormatTable(string[] lines, IHasRows hasRows, string indent, string newLine)
        {
            var widths = AutoFormatTableCommand.GetWidths(hasRows);
            foreach (var row in hasRows.Rows)
            {
                var result = new StringBuilder();
                result.Append(indent);
                result.Append("|");
                foreach (var item in row.Cells.Select((c, i) => new { c, i }))
                {
                    result.Append(new string(' ', PADDING_LENGHT));
                    result.Append(AutoFormatTableCommand.Escape(item.c.Value).PadRight(widths[item.i]));
                    result.Append(new string(' ', PADDING_LENGHT));
                    result.Append('|');
                }

                SetLine(lines, row, result.ToString());
            }
        }

        private void SetLine(string[] lines, IHasLocation hasLocation, string line)
        {
            if (hasLocation?.Location != null && hasLocation.Location.Line >= 1 
                                              && hasLocation.Location.Line - 1 < line.Length)
            {
                lines[hasLocation.Location.Line - 1] = line;
            }
        }
    }
}