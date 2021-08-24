using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
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
        private const int PADDING_LENGTH = 1;

        internal static readonly DeveroomEditorCommandTargetKey FormatDocumentKey =
                    new(VSConstants.VSStd2K, VSConstants.VSStd2KCmdID.FORMATDOCUMENT);
        internal static readonly DeveroomEditorCommandTargetKey FormatSelectionKey =
                    new(VSConstants.VSStd2K, VSConstants.VSStd2KCmdID.FORMATSELECTION);

        public override DeveroomEditorCommandTargetKey[] Targets => new[]
        {
            FormatDocumentKey,
            FormatSelectionKey
        };

        [ImportingConstructor]
        public AutoFormatDocumentCommand(IIdeScope ideScope, IBufferTagAggregatorFactoryService aggregatorFactory, IMonitoringService monitoringService) : base(ideScope, aggregatorFactory, monitoringService)
        {
        }

        class DocumentLinesEditBuffer
        {
            private readonly int _startLine;
            private readonly int _endLine;
            private readonly string[] _lines;

            public bool IsEmpty => _lines.Length == 0;

            public DocumentLinesEditBuffer(ITextSnapshot textSnapshot, int startLine, int endLine)
            {
                _startLine = startLine;
                _endLine = endLine;
                _lines = GetSpanFullLines(textSnapshot, startLine, endLine)
                    .Select(l => l.GetText()).ToArray();
            }

            private string GetLineZeroBased(int zeroBasedLineNumber)
            {
                if (zeroBasedLineNumber < _startLine || zeroBasedLineNumber > _endLine)
                    return string.Empty;
                return _lines[zeroBasedLineNumber - _startLine];
            }

            public string GetLine(int lineNumber) => GetLineZeroBased(lineNumber - 1);

            private void SetLineZeroBased(int zeroBasedLineNumber, string line)
            {
                if (zeroBasedLineNumber < _startLine || zeroBasedLineNumber > _endLine)
                    return;
                _lines[zeroBasedLineNumber - _startLine] = line;
            }

            public void SetLine(int lineNumber, string line) => SetLineZeroBased(lineNumber - 1, line);

            public string GetModifiedText(string newLine)
            {
                return string.Join(newLine, _lines);
            }
        }

        class GherkinFormatSettings
        {
            public string Indent { get; set; } = "    ";

            public int FeatureChildrenIndentLevel { get; set; } = 0;

            public int RuleChildrenIndentLevelWithinRule { get; set; } = 0;

            public int StepIndentLevelWithinStepContainer { get; set; } = 1;

            public int AndStepIndentLevelWithinSteps { get; set; } = 0;

            public int DataTableIndentLevelWithinStep { get; set; } = 1;
            public int DocStringIndentLevelWithinStep { get; set; } = 1;

            public int ExamplesBlockIndentLevelWithinScenarioOutline { get; set; } = 0;

            public int ExamplesTableIndentLevelWithinExamplesBlock { get; set; } = 1;
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

            var formatSettings = new GherkinFormatSettings();//todo: take from config

            var lines = new DocumentLinesEditBuffer(textSnapshot, startLine, endLine);
            if (lines.IsEmpty)
                return false;
            
            using (var textEdit = textSnapshot.TextBuffer.CreateEdit())
            {
                var formattingSpan = new SnapshotSpan(textSnapshot.GetLineFromLineNumber(startLine).Start, textSnapshot.GetLineFromLineNumber(endLine).End);
                var newLine = GetNewLine(textView);
                SetFormattedLines(lines, gherkinDocument, formatSettings);

                var replacementText = lines.GetModifiedText(newLine);

                textEdit.Replace(formattingSpan, replacementText);
                textEdit.Apply();
            }

            textView.Caret.MoveTo(textView.TextSnapshot.GetLineFromLineNumber(caretLineNumber).End);

            return true;
        }
        
        private void SetFormattedLines(DocumentLinesEditBuffer lines, DeveroomGherkinDocument gherkinDocument, GherkinFormatSettings formatSettings)
        {
            if (gherkinDocument.Feature != null)
            {
                SetTagsAndLine(lines, gherkinDocument.Feature, string.Empty);
                SetLinesForChildren(lines, gherkinDocument.Feature.Children, formatSettings, formatSettings.FeatureChildrenIndentLevel);
            }
        }

        private void SetLinesForChildren(DocumentLinesEditBuffer lines, IEnumerable<IHasLocation> hasLocation, GherkinFormatSettings formatSettings, int indentLevel)
        {
            foreach (var featureChild in hasLocation)
            {
                SetTagsAndLine(lines, featureChild, GetIndent(formatSettings, indentLevel));

                if (featureChild is Rule rule)
                {
                    SetLinesForChildren(lines, rule.Children, formatSettings, indentLevel + formatSettings.RuleChildrenIndentLevelWithinRule);
                }

                if (featureChild is ScenarioOutline scenarioOutline)
                {
                    foreach (var example in scenarioOutline.Examples)
                    {
                        var examplesBlockIndentLevel = indentLevel + formatSettings.ExamplesBlockIndentLevelWithinScenarioOutline;
                        SetTagsAndLine(lines, example, GetIndent(formatSettings, examplesBlockIndentLevel));
                        FormatTable(lines, example, formatSettings, examplesBlockIndentLevel + formatSettings.ExamplesTableIndentLevelWithinExamplesBlock);
                    }
                }

                if (featureChild is IHasSteps hasSteps)
                {
                    foreach (var step in hasSteps.Steps)
                    {
                        var stepIndentLevel = indentLevel + formatSettings.StepIndentLevelWithinStepContainer;
                        if (step is DeveroomGherkinStep deveroomGherkinStep &&
                            (deveroomGherkinStep.StepKeyword == StepKeyword.And ||
                             deveroomGherkinStep.StepKeyword == StepKeyword.But))
                            stepIndentLevel += formatSettings.AndStepIndentLevelWithinSteps;
                        SetLine(lines, step, $"{GetIndent(formatSettings, stepIndentLevel)}{step.Keyword}{step.Text}");
                        if (step.Argument is DataTable dataTable)
                        {
                            FormatTable(lines, dataTable, formatSettings, stepIndentLevel + formatSettings.DataTableIndentLevelWithinStep);
                        }

                        if (step.Argument is DocString docString)
                        {
                            FormatDocString(lines, docString, formatSettings, stepIndentLevel + formatSettings.DocStringIndentLevelWithinStep);
                        }
                    }
                }
            }
        }

        private void SetTagsAndLine(DocumentLinesEditBuffer lines, IHasLocation hasLocation, string indent)
        {
            if (hasLocation is IHasTags hasTags)
            {
                SetTags(lines, hasTags.Tags, indent);
            }

            if (hasLocation is IHasDescription hasDescription)
            {
                SetLine(lines, hasLocation, GetHasDescriptionLine(hasDescription, indent));
            }
        }

        private void FormatTable(DocumentLinesEditBuffer lines, IHasRows hasRows, GherkinFormatSettings formatSettings, int indentLevel)
        {
            var indent = GetIndent(formatSettings, indentLevel);
            var widths = AutoFormatTableCommand.GetWidths(hasRows);
            foreach (var row in hasRows.Rows)
            {
                var result = new StringBuilder();
                result.Append(indent);
                result.Append("|");
                foreach (var item in row.Cells.Select((c, i) => new { c, i }))
                {
                    result.Append(new string(' ', PADDING_LENGTH));
                    result.Append(AutoFormatTableCommand.Escape(item.c.Value).PadRight(widths[item.i]));
                    result.Append(new string(' ', PADDING_LENGTH));
                    result.Append('|');
                }

                SetLine(lines, row, result.ToString());
            }
        }

        private void FormatDocString(DocumentLinesEditBuffer lines, DocString docString, GherkinFormatSettings formatSettings, int indentLevel)
        {
            var indent = GetIndent(formatSettings, indentLevel);
            var docStringStartLine = docString.Location.Line;
            var docStringContentLines = DeveroomTagParser.NewLineRe.Split(docString.Content);
            if (string.IsNullOrEmpty(docString.Content) &&
                !string.IsNullOrWhiteSpace(lines.GetLine(docStringStartLine +1)))
            {
                docStringContentLines = Array.Empty<string>();
            }

            var docStringEndLine = docStringStartLine + docStringContentLines.Length + 1;
            var delimiterLine = $"{indent}{docString.Delimiter}";

            lines.SetLine(docStringStartLine, delimiterLine);
            var docStringRow = 1;
            foreach (var contentLine in docStringContentLines)
            {
                var line = $"{indent}{contentLine}";
                lines.SetLine(docStringStartLine + docStringRow++, line);
            }
            lines.SetLine(docStringEndLine, delimiterLine);
        }

        private string GetHasDescriptionLine(IHasDescription hasDescription, string indent)
        {
            var line = $"{indent}{hasDescription.Keyword}:";
            if (!string.IsNullOrEmpty(hasDescription.Name))
                line += $" {hasDescription.Name}";
            return line;
        }

        private void SetTags(DocumentLinesEditBuffer lines, IEnumerable<Tag> tags, string indent)
        {
            var tagGroup = tags.GroupBy(t => t.Location.Line);
            foreach (var tag in tagGroup)
            {
                var line = indent + string.Join(" ", tag.Select(t => t.Name));
                lines.SetLine(tag.Key, line);
            }
        }

        private void SetLine(DocumentLinesEditBuffer lines, IHasLocation hasLocation, string line)
        {
            if (hasLocation?.Location != null && hasLocation.Location.Line >= 1
                                              && hasLocation.Location.Column - 1 < line.Length)
            {
                lines.SetLine(hasLocation.Location.Line, line);
            }
        }

        private string GetIndent(GherkinFormatSettings formatSettings, int indentLevel)
        {
            if (indentLevel == 0)
                return string.Empty;
            if (indentLevel == 1)
                return formatSettings.Indent;
            return string.Join(string.Empty, Enumerable.Range(0, indentLevel).Select(_ => formatSettings.Indent));
        }
    }
}