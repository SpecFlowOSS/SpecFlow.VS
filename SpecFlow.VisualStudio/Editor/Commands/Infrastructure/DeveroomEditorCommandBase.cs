using System;
using System.Collections.Generic;
using System.Linq;
using SpecFlow.VisualStudio.Diagnostics;
using SpecFlow.VisualStudio.Editor.Services;
using SpecFlow.VisualStudio.Monitoring;
using SpecFlow.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.OptionsExtensionMethods;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.TextManager.Interop;
using SpecFlow.VisualStudio.Configuration;
using SpecFlow.VisualStudio.ProjectSystem.Configuration;

namespace SpecFlow.VisualStudio.Editor.Commands.Infrastructure
{
    public abstract class DeveroomEditorCommandBase : IDeveroomEditorCommand
    {
        protected readonly IIdeScope IdeScope;
        protected readonly IBufferTagAggregatorFactoryService AggregatorFactory;
        protected readonly IMonitoringService MonitoringService;

        public virtual DeveroomEditorCommandTargetKey Target 
            => throw new NotImplementedException();

        public virtual DeveroomEditorCommandTargetKey[] Targets 
            => new [] { Target };

        protected IDeveroomLogger Logger => IdeScope.Logger;

        protected DeveroomEditorCommandBase(IIdeScope ideScope, IBufferTagAggregatorFactoryService aggregatorFactory, IMonitoringService monitoringService)
        {
            IdeScope = ideScope;
            AggregatorFactory = aggregatorFactory;
            MonitoringService = monitoringService;
        }

        protected DeveroomTag GetDeveroomTagForCaret(IWpfTextView textView, params string[] tagTypes)
        {
            var tagger = DeveroomTaggerProvider.GetDeveroomTagger(textView.TextBuffer);
            if (tagger == null) return null;
            var tag = DumpDeveroomTags(tagger.GetDeveroomTagsForCaret(textView)).FirstOrDefault(t => tagTypes.Contains(t.Type));
            if (tag != null &&
                tag.Span.Snapshot.Version.VersionNumber != textView.TextSnapshot.Version.VersionNumber)
            {
                Logger.LogVerbose("Snapshot version mismatch");
                tagger.InvalidateCache();
                tag = DumpDeveroomTags(tagger.GetDeveroomTagsForCaret(textView)).FirstOrDefault(t => tagTypes.Contains(t.Type));
            }
            return tag;
        }

        protected IEnumerable<DeveroomTag> DumpDeveroomTags(IEnumerable<DeveroomTag> deveroomTags)
        {
#if DEBUG
            foreach (var deveroomTag in deveroomTags)
            {
                Logger.LogVerbose($"  Tag: {deveroomTag.Type}");
                yield return deveroomTag;
            }
#else
            return deveroomTags;
#endif
        }

        protected string GetEditorDocumentPath(ITextBuffer textBuffer)
        {
            if (!textBuffer.Properties.TryGetProperty(typeof(IVsTextBuffer), out IVsTextBuffer bufferAdapter))
                return null;

            if (!(bufferAdapter is IPersistFileFormat persistFileFormat))
                return null;

            if (!ErrorHandler.Succeeded(persistFileFormat.GetCurFile(out string filePath, out _)))
                return null;

            return filePath;
        }

        protected DeveroomConfiguration GetConfiguration(IWpfTextView textView)
        {
            var configuration = IdeScope.GetDeveroomConfiguration(IdeScope.GetProject(textView.TextBuffer));
            return configuration;
        }

        public virtual DeveroomEditorCommandStatus QueryStatus(IWpfTextView textView, DeveroomEditorCommandTargetKey commandKey)
        {
            return DeveroomEditorCommandStatus.Supported;
        }

        public virtual bool PreExec(IWpfTextView textView, DeveroomEditorCommandTargetKey commandKey, IntPtr inArgs = default(IntPtr))
        {
            return false;
        }

        public virtual bool PostExec(IWpfTextView textView, DeveroomEditorCommandTargetKey commandKey, IntPtr inArgs = default(IntPtr))
        {
            return false;
        }

        #region Helper methods
        protected void SetSelectionToChangedLines(IWpfTextView textView, ITextSnapshotLine[] lines)
        {
            var newSnapshot = textView.TextBuffer.CurrentSnapshot;
            var selectionStartPosition = newSnapshot.GetLineFromLineNumber(lines.First().LineNumber).Start;
            var selectionEndPosition = newSnapshot.GetLineFromLineNumber(lines.Last().LineNumber).End;
            textView.Selection.Select(new SnapshotSpan(
                selectionStartPosition,
                selectionEndPosition), false);
            textView.Caret.MoveTo(selectionEndPosition);
        }

        protected SnapshotSpan GetSelectionSpan(IWpfTextView textView)
        {
            return new SnapshotSpan(textView.Selection.Start.Position, textView.Selection.End.Position);
        }

        protected IEnumerable<ITextSnapshotLine> GetSpanFullLines(SnapshotSpan span)
        {
            var selectionStartLine = span.Start.GetContainingLine();
            var selectionEndLine = GetSelectionEndLine(selectionStartLine, span);
            return GetSpanFullLines(selectionStartLine.Snapshot, selectionStartLine.LineNumber, selectionEndLine.LineNumber);
        }

        internal static IEnumerable<ITextSnapshotLine> GetSpanFullLines(ITextSnapshot textSnapshot, int startLine, int endLine)
        {
            for (int lineNumber = startLine; lineNumber <= endLine; lineNumber++)
            {
                yield return textSnapshot.GetLineFromLineNumber(lineNumber);
            }
        }

        protected IEnumerable<ITextSnapshotLine> GetSpanFullLines(ITextSnapshot textSnapshot)
        {
            for (int lineNumber = 0; lineNumber < textSnapshot.LineCount; lineNumber++)
            {
                yield return textSnapshot.GetLineFromLineNumber(lineNumber);
            }
        }

        private ITextSnapshotLine GetSelectionEndLine(ITextSnapshotLine selectionStartLine, SnapshotSpan span)
        {
            var selectionEndLine = span.End.GetContainingLine();
            // if the selection ends exactly at the beginning of a new line (ie line select), we do not comment out the last line
            if (selectionStartLine.LineNumber != selectionEndLine.LineNumber && selectionEndLine.Start.Equals(span.End))
            {
                selectionEndLine = selectionEndLine.Snapshot.GetLineFromLineNumber(selectionEndLine.LineNumber - 1);
            }
            return selectionEndLine;
        }

        protected string GetNewLine(IWpfTextView textView)
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
        #endregion
    }
}