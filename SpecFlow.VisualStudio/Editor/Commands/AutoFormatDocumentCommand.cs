using System;
using System.Collections.Generic;
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

        internal static readonly DeveroomEditorCommandTargetKey FormatDocumentKey =
                    new DeveroomEditorCommandTargetKey(VSConstants.VSStd2K, VSConstants.VSStd2KCmdID.FORMATDOCUMENT);
        internal static readonly DeveroomEditorCommandTargetKey FormatSelectionKey =
                    new DeveroomEditorCommandTargetKey(VSConstants.VSStd2K, VSConstants.VSStd2KCmdID.FORMATSELECTION);

        public override DeveroomEditorCommandTargetKey[] Targets => new[]
        {
            FormatDocumentKey,
            FormatSelectionKey
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

            var isSelectionFormatting = commandKey.Equals(FormatSelectionKey);
            var textSnapshot = textView.TextSnapshot;
            var caretLineNumber = textView.Caret.Position.BufferPosition.GetContainingLine().LineNumber;
            
            var startLine = 0;
            var endLine = textSnapshot.LineCount - 1;
            
            if (isSelectionFormatting)
            {
                var selectionSpan = GetSelectionSpan(textView);
                startLine = selectionSpan.Start.GetContainingLine().LineNumber;
                endLine = selectionSpan.End.GetContainingLine().LineNumber;
            }

            var lines = GetSpanFullLines(textSnapshot).Select(l => l.GetText()).ToArray();
            if (lines.Length == 0)
                return false;
            
            string indent = "    "; //todo: take from config
            using (var textEdit = textSnapshot.TextBuffer.CreateEdit())
            {
                var formattingSpan = new SnapshotSpan(textSnapshot.GetLineFromLineNumber(startLine).Start, textSnapshot.GetLineFromLineNumber(endLine).End);
                var newLine = GetNewLine(textView);
                SetFormattedLines(lines, gherkinDocument, newLine, indent);

                var replacementText = string.Join(newLine, lines.Take(endLine + 1)
                    .Skip(startLine));

                textEdit.Replace(formattingSpan, replacementText);
                textEdit.Apply();
            }

            textView.Caret.MoveTo(textView.TextSnapshot.GetLineFromLineNumber(caretLineNumber).End);

            return true;
        }
        
        private void SetFormattedLines(string[] lines, DeveroomGherkinDocument gherkinDocument, string newLine, string indent)
        {
            if (gherkinDocument.Feature != null)
            {
                SetTagsAndLine(lines, gherkinDocument.Feature);
                SetLinesForChildren(lines, gherkinDocument.Feature.Children, indent, newLine);
            }
        }

        private void SetLinesForChildren(string[] lines, IEnumerable<IHasLocation> hasLocation, string indent, string newLine)
        {
            foreach (var featureChild in hasLocation)
            {
                SetTagsAndLine(lines, featureChild);

                if (featureChild is Rule rule)
                {
                    SetLinesForChildren(lines, rule.Children, indent, newLine);
                }

                if (featureChild is ScenarioOutline scenarioOutline)
                {
                    foreach (var example in scenarioOutline.Examples)
                    {
                        SetTagsAndLine(lines, example);
                        FormatTable(lines, example, indent);
                    }
                }

                if (featureChild is IHasSteps hasSteps)
                {
                    foreach (var step in hasSteps.Steps)
                    {
                        SetLine(lines, step, $"{indent}{step.Keyword}{step.Text}");
                        if (step.Argument is DataTable dataTable)
                        {
                            FormatTable(lines, dataTable, indent + indent);
                        }

                        if (step.Argument is DocString docString)
                        {
                            FormatDocString(lines, indent, docString, newLine);
                        }
                    }
                }
            }
        }

        private void SetTagsAndLine(string[] lines, IHasLocation hasLocation)
        {
            if (hasLocation is IHasTags hasTags)
            {
                SetTags(lines, hasTags.Tags);
            }

            if (hasLocation is IHasDescription hasDescription)
            {
                SetLine(lines, hasLocation, GetHasDescriptionLine(hasDescription));
            }
        }

        private void FormatTable(string[] lines, IHasRows hasRows, string indent)
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

        private void FormatDocString(string[] lines, string indent, DocString docString, string newLine)
        {
            var docStringStartLine = docString.Location.Line;
            var docStringContentLines = docString.Content.Split(new[] { newLine }, StringSplitOptions.None);
            var docStringEndLine = docStringStartLine + docStringContentLines.Length + 1;
            var delimiterLine = $"{indent + indent}{docString.Delimiter}";

            SetLine(lines, docStringStartLine, delimiterLine);
            var docStringRow = 1;
            foreach (var contentLine in docStringContentLines)
            {
                var line = $"{indent + indent}{contentLine}";
                SetLine(lines, docStringStartLine + docStringRow++, line);
            }
            SetLine(lines, docStringEndLine, delimiterLine);
        }

        private string GetHasDescriptionLine(IHasDescription hasDescription)
        {
            var line = $"{hasDescription.Keyword}:";
            if (!string.IsNullOrEmpty(hasDescription.Name))
                line += $" {hasDescription.Name}";
            return line;
        }

        private void SetTags(string[] lines, IEnumerable<Tag> tags)
        {
            var tagGroup = tags.GroupBy(t => t.Location.Line);
            foreach (var tag in tagGroup)
            {
                var line = string.Join(" ", tag.Select(t => t.Name));
                SetLine(lines, tag.Key, line);
            }
        }

        private void SetLine(string[] lines, IHasLocation hasLocation, string line)
        {
            if (hasLocation?.Location != null && hasLocation.Location.Line >= 1
                                              && hasLocation.Location.Column - 1 < line.Length)
            {
                SetLine(lines, hasLocation.Location.Line, line);
            }
        }

        private void SetLine(string[] lines, int lineNumber, string line)
        {
            lines[lineNumber - 1] = line;
        }
    }
}