using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Deveroom.VisualStudio.Editor.Commands.Infrastructure;
using Deveroom.VisualStudio.Editor.Services;
using Deveroom.VisualStudio.Monitoring;
using Deveroom.VisualStudio.ProjectSystem;
using Gherkin.Ast;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.OptionsExtensionMethods;
using Microsoft.VisualStudio.Text.Tagging;

namespace Deveroom.VisualStudio.Editor.Commands
{
    [Export(typeof(IDeveroomFeatureEditorCommand))]
    public class AutoFormatTableCommand : DeveroomEditorTypeCharCommandBase, IDeveroomFeatureEditorCommand
    {
        private struct TableCaretPosition
        {
            public readonly int Line;
            public readonly int Column;
            public readonly int? Cell;

            public bool IsUnknown => Cell == null;
            public bool IsBeforeFirstCell => Cell == null;
            public bool IsAfterLastCell => Cell == int.MaxValue;
            public bool IsInCell => !IsUnknown && !IsBeforeFirstCell && !IsAfterLastCell;

            private TableCaretPosition(int line, int column, int? cell)
            {
                Line = line;
                Column = column;
                Cell = cell;
            }

            public static TableCaretPosition CreateUnknown(int line, int column) => new TableCaretPosition(line, column, null);
            public static TableCaretPosition CreateBeforeFirstCell(int line, int column) => new TableCaretPosition(line, column, null);
            public static TableCaretPosition CreateInCell(int line, int cell, int withinCellColumn) => new TableCaretPosition(line, withinCellColumn, cell);
            public static TableCaretPosition CreateAfterLastCell(int line) => new TableCaretPosition(line, 0, int.MaxValue);
        }

        private const int PADDING_LENGHT = 1;
        private const int PIPE_LENGHT = 1;

        [ImportingConstructor]
        public AutoFormatTableCommand(IIdeScope ideScope, IBufferTagAggregatorFactoryService aggregatorFactory, IMonitoringService monitoringService) : base(ideScope, aggregatorFactory, monitoringService)
        {
        }

        protected internal override bool PostExec(IWpfTextView textView, char ch)
        {
            if (ch != '|')
                return false;

            var dataTableTag = GetDeveroomTagForCaret(textView, DeveroomTagTypes.DataTable, DeveroomTagTypes.ExamplesBlock);
            if (!(dataTableTag?.Data is IHasRows hasRows))
                return false;

            var currentText = dataTableTag.Span.GetText();
            if (IsEscapedPipeTyped(currentText, textView.Caret.Position.BufferPosition))
                return false;

            var indent = GetIndent(textView.TextSnapshot, hasRows);
            var newLine = GetNewLine(textView);

            var caretPosition = GetCaretPosition(textView.Caret.Position, hasRows);
            var formattedTableText = GetFormattedTableText(hasRows, indent, newLine, caretPosition, textView.TextSnapshot, out var newCaretLineColumn);
            if (currentText.Equals(formattedTableText))
                return false;

            MonitoringService.MonitorCommandAutoFormatTable();

            using (var textEdit = textView.TextBuffer.CreateEdit())
            {
                textEdit.Replace(dataTableTag.Span, formattedTableText);
                textEdit.Apply();
            }

            RestoreCaretPosition(textView, caretPosition, newCaretLineColumn);
            return true;
        }

        private bool IsEscapedPipeTyped(string currentText, SnapshotPoint positionBufferPosition)
        {
            positionBufferPosition = positionBufferPosition.Subtract(1);
            int backslashCount = 0;
            while (positionBufferPosition.Position > 0)
            {
                positionBufferPosition = positionBufferPosition.Subtract(1);
                if (positionBufferPosition.GetChar() != '\\')
                    break;
                backslashCount++;
            }
            return backslashCount % 2 == 1;
        }

        private string GetNewLine(IWpfTextView textView)
        {
            // based on EditorOperations.InsertNewLine()
            string newLineString = null;
            if (textView.Options.GetReplicateNewLineCharacter())
            {
                var caretLine = textView.Caret.Position.BufferPosition.GetContainingLine();
                if (caretLine.LineBreakLength > 0)
                    newLineString = caretLine.GetLineBreakText();
                else if (textView.TextSnapshot.LineCount > 1)
                {
                    newLineString = textView.TextSnapshot.GetLineFromLineNumber(textView.TextSnapshot.LineCount - 2).GetLineBreakText();
                }
            }
            newLineString = newLineString ?? textView.Options.GetNewLineCharacter();
            return newLineString;
        }

        private string GetIndent(ITextSnapshot textSnapshot, IHasRows hasRows)
        {
            var firstLine = textSnapshot.GetLineFromLineNumber(hasRows.Rows.First().Location.Line - 1);
            return Regex.Match(firstLine.GetText(), @"^\s*").Value;
        }

        private void RestoreCaretPosition(IWpfTextView textView, TableCaretPosition caretPosition, int newCaretLineColumn)
        {
            var line = textView.TextSnapshot.GetLineFromLineNumber(caretPosition.Line);
            var lineStartPosition = line.Start.Position;
            textView.Caret.MoveTo(new SnapshotPoint(textView.TextSnapshot,
                Math.Min(lineStartPosition + newCaretLineColumn, line.End.Position)));
        }

        private TableCaretPosition GetCaretPosition(CaretPosition caretPosition, IHasRows hasRows)
        {
            var line = caretPosition.BufferPosition.GetContainingLine();
            var caretColumn = caretPosition.BufferPosition.Position - line.Start.Position;
            var row = hasRows.Rows.FirstOrDefault(r => r.Location.Line == line.LineNumber + 1);
            if (row == null || row.Cells.Any(c => c.Location.Column <= 0))
                return TableCaretPosition.CreateUnknown(line.LineNumber, caretColumn);

            var cellIndex = FindCellIndex(caretColumn, row, line.GetText(), out var cell);
            if (cellIndex < 0) // before first cell
                return TableCaretPosition.CreateBeforeFirstCell(line.LineNumber, caretColumn);
            if (cellIndex == int.MaxValue) // after last cell
                return TableCaretPosition.CreateAfterLastCell(line.LineNumber);

            return TableCaretPosition.CreateInCell(line.LineNumber, cellIndex,
                Math.Max(caretColumn - (cell.Location.Column - 1), 0));
        }

        private int FindCellIndex(int caretColumn, TableRow row, string lineText, out TableCell cell)
        {
            cell = null;
            int cellIndex = -1;
            int lastCellStart = 0;
            foreach (var tableCell in row.Cells)
            {
                if (tableCell.Location.Column - 1 > caretColumn)
                {
                    // either the previous cell or the padding of this cell
                    if (!ContainsPipe(lineText.Substring(lastCellStart, caretColumn - lastCellStart)))
                        return cellIndex; // previous cell

                    cell = tableCell;
                    return cellIndex + 1;
                }

                cell = tableCell;
                lastCellStart = tableCell.Location.Column - 1;
                cellIndex++;
            }

            // either the last cell or after last
            if (!ContainsPipe(lineText.Substring(lastCellStart, caretColumn - lastCellStart)))
                return cellIndex; // last cell

            cell = null;
            return Int32.MaxValue;
        }

        private bool ContainsPipe(string s)
        {
            return FindPipeIndex(s) >= 0;
        }

        private int FindPipeIndex(string s)
        {
            var pipeMatch = Regex.Match(s, @"(?<![^\\](\\\\)*\\)\|");
            return pipeMatch.Success ? pipeMatch.Index : -1;
        }

        private string GetFormattedTableText(IHasRows hasRows, string indent, string newLine, TableCaretPosition caretPosition, ITextSnapshot textSnapshot, out int newCaretLinePosition)
        {
            var widths = GetWidths(hasRows);

            int nextLine = ((IHasLocation)hasRows).Location.Line;
            var result = new StringBuilder();
            foreach (var row in hasRows.Rows)
            {
                while (row.Location.Line > nextLine)
                {
                    var nonRowLine = textSnapshot.GetLineFromLineNumber(nextLine - 1);
                    result.Append(nonRowLine.GetText());
                    result.Append(newLine);
                    nextLine++;
                }

                result.Append(indent);
                result.Append("|");
                foreach (var item in row.Cells.Select((c, i) => new { c, i }))
                {
                    result.Append(new string(' ', PADDING_LENGHT));
                    result.Append(Escape(item.c.Value).PadRight(widths[item.i]));
                    result.Append(new string(' ', PADDING_LENGHT));
                    result.Append('|');
                }

                var lineText = textSnapshot.GetLineFromLineNumber(nextLine - 1).GetText();
                var unfinishedCell = GetUnfinishedCell(lineText);
                if (unfinishedCell != null)
                {
                    result.Append(' ');
                    result.Append(unfinishedCell);
                }

                result.Append(newLine);
                nextLine++;
            }
            result.Remove(result.Length - newLine.Length, newLine.Length);

            newCaretLinePosition = CalculateNewCaretLinePosition(caretPosition, widths, indent);

            return result.ToString();
        }

        private static string GetUnfinishedCell(string lineText)
        {
            var match = Regex.Match(lineText, @"(?<!\\)(\\\\)*\|(?<remaining>.*?)$", RegexOptions.RightToLeft);
            string unfinishedCell = null;
            if (match.Success && !string.IsNullOrWhiteSpace(match.Groups["remaining"].Value))
            {
                unfinishedCell = match.Groups["remaining"].Value.Trim();
            }

            return unfinishedCell;
        }

        private int[] GetWidths(IHasRows hasRows)
        {
            var widths = new int[hasRows.Rows.Max(r => r.Cells.Count())];
            foreach (var row in hasRows.Rows)
            {
                foreach (var item in row.Cells.Select((c, i) => new {c, i}))
                {
                    widths[item.i] = Math.Max(widths[item.i], Escape(item.c.Value).Length);
                }
            }
            return widths;
        }

        private int CalculateNewCaretLinePosition(TableCaretPosition caretPosition, int[] widths, string indent)
        {
            if (caretPosition.IsUnknown)
            {
                return caretPosition.Column;
            }
            if (caretPosition.IsBeforeFirstCell)
            {
                return Math.Min(caretPosition.Column, indent.Length);
            }

            Debug.Assert(caretPosition.Cell != null);
            var positionAfterCellOpenPipe =
                indent.Length +
                widths.Take(Math.Min(widths.Length, caretPosition.Cell.Value))
                    .Sum(w => w + PADDING_LENGHT * 2 + PIPE_LENGHT) +
                PIPE_LENGHT;

            if (caretPosition.IsAfterLastCell)
                return positionAfterCellOpenPipe; // position after the last cell pipe (position of the open pipe of the one after last cell)

            Debug.Assert(caretPosition.IsInCell);
            // position within the cell + 2*padding, if cell has been shrinked, we move position to end of cell (Math.Min)
            return positionAfterCellOpenPipe +
                PADDING_LENGHT + 
                Math.Min(caretPosition.Column, widths[caretPosition.Cell.Value] + 
                    PADDING_LENGHT);
        }

        private string Escape(string cellValue)
        {
            return cellValue
                .Replace("\\", "\\\\")
                .Replace("\r", "\\r")
                .Replace("\n", "\\n")
                .Replace("|", "\\|");
        }
    }
}