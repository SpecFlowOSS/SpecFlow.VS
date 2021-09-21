using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using FluentAssertions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace SpecFlow.VisualStudio.VsxStubs
{
    public class TestTextPosition
    {
        public int Line { get; }
        public int Column { get; }

        public TestTextPosition(int line, int column)
        {
            Line = line;
            Column = column;
        }

        public override string ToString()
        {
            return $"({Line},{Column})";
        }
    }

    public class TestTextSection
    {
        private readonly TestText _testText;
        public string Label { get; }
        public TestTextPosition Start { get; }
        public TestTextPosition End { get; set; }
        public string Text => _testText.GetText(Start, End);

        public TestTextSection(TestText testText, string label, TestTextPosition start, TestTextPosition end = null)
        {
            _testText = testText;
            Label = label;
            Start = start;
            End = end;
        }

        public override string ToString()
        {
            return $"{Label}{Start}-{End}:{Text}";
        }
    }

    public class TestText
    {
        private const int NewLineLenght = 2;

        private readonly string[] _lines;

        public string[] Lines => _lines;
        public TestTextPosition CaretPosition => Sections.FirstOrDefault(s => s.Label == "caret")?.Start;
        public TestTextSection CaretSelection => Sections.FirstOrDefault(s => s.Label == "sel");
        public List<TestTextSection> Sections = new List<TestTextSection>();

        public TestText(params string[] valueLines)
        {
            var value = string.Join(Environment.NewLine, valueLines);
            _lines = ProcessValue(value);
        }


        private string[] ProcessValue(string value)
        {
            var result = value.Split(new[] {Environment.NewLine}, StringSplitOptions.None);
            for (int lineIndex = 0; lineIndex < result.Length; lineIndex++)
            {
                var markerMatches = Regex.Matches(result[lineIndex], @"{\/?(?<label>\w+)}").Cast<Match>();
                int removedChars = 0;
                foreach (var markerMatch in markerMatches)
                {
                    bool isClose = markerMatch.Value.StartsWith("{/");
                    var label = markerMatch.Groups["label"].Value;
                    TestTextPosition position = new TestTextPosition(lineIndex, markerMatch.Index - removedChars);
                    if (isClose)
                    {
                        var section = Sections.LastOrDefault(s => s.Label == label);
                        if (section == null)
                        {
                            section = new TestTextSection(this, label, position);
                            Sections.Add(section);
                        }
                        section.End = position;
                        //section.Text = GetText(section.Start, section.End);
                    }
                    else
                    {
                        var section = new TestTextSection(this, label, position);
                        Sections.Add(section);
                    }

                    result[lineIndex] = result[lineIndex].Remove(position.Column, markerMatch.Length);
                    removedChars += markerMatch.Length;
                }
            }
            return result;
        }

        public string GetText(TestTextPosition start, TestTextPosition end)
        {
            if (start.Line == end.Line)
            {
                return _lines[start.Line].Substring(start.Column, end.Column - start.Column);
            }
            var result = new StringBuilder();
            result.AppendLine(_lines[start.Line].Substring(start.Column));
            for (int i = start.Line + 1; i <= end.Line - 1; i++)
            {
                result.AppendLine(_lines[i]);
            }
            result.Append(_lines[end.Line].Substring(0, end.Column));
            return result.ToString();
        }

        public SnapshotPoint GetSnapshotPoint(ITextSnapshot textSnapshot, int line, int column = 0)
        {
            line = line < 0 ? Lines.Length + line : line;
            column = column == -1 ? Lines[line].Length : column;
            return new SnapshotPoint(textSnapshot, GetLineStartPosition(line) + column);
        }

        public int GetLineStartPosition(int lineNo)
        {
            if (lineNo == 0)
                return 0;
            return _lines.Take(lineNo).Sum(l => l.Length + NewLineLenght);
        }

        public int GetLineEndPosition(int lineNo, bool includingNl = false)
        {
            var start = GetLineStartPosition(lineNo);
            return start + _lines[lineNo].Length + (includingNl ? NewLineLenght : 0);
        }

        public override string ToString()
        {
            return ToString(null);
        }

        public string ToString(string newLine)
        {
            return string.Join(newLine ?? Environment.NewLine, Lines);
        }

        public void SetCaret(IWpfTextView textView)
        {
            if (CaretPosition != null)
                MoveCaretTo(textView, CaretPosition.Line, CaretPosition.Column);
        }

        public void MoveCaretTo(IWpfTextView textView, int line, int column)
        {
            textView.Caret.MoveTo(GetSnapshotPoint(textView.TextSnapshot, line, column));
        }

        public void AssertCaretAt(IWpfTextView textView, int line, int column)
        {
            textView.Caret.Position.BufferPosition.Position.Should().Be(GetSnapshotPoint(textView.TextSnapshot, line, column));
        }

        public void SetSelection(StubWpfTextView textView)
        {
            var selection = CaretSelection;
            if (selection == null)
                return;
            textView.Selection.Select(new SnapshotSpan(
                    GetSnapshotPoint(textView.TextSnapshot, selection.Start.Line, selection.Start.Column),
                    GetSnapshotPoint(textView.TextSnapshot, selection.End.Line, selection.End.Column)),
                false);
        }

        public TestText Replace(int line, string what, string to = null)
        {
            line = line < 0 ? Lines.Length + line : line;
            to = to ?? "";
            return new TestText(
                Lines.Select((l, i) => 
                    i == line ? l.Replace(what, to) : l).ToArray());
        }
    }
}
