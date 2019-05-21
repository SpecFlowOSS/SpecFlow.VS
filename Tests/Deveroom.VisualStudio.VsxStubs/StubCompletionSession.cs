using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace Deveroom.VisualStudio.VsxStubs
{
    public class StubCompletionSession : ICompletionSession
    {
        public PropertyCollection Properties { get; } = new PropertyCollection();
        public ITextView TextView { get; }
        public IIntellisensePresenter Presenter { get; }
        public bool IsDismissed { get; }
        public ReadOnlyObservableCollection<CompletionSet> CompletionSets { get; }
        public CompletionSet SelectedCompletionSet
        {
            get => CompletionSets.FirstOrDefault();
            set => throw new NotImplementedException();
        }
        public bool IsStarted { get; }

        public event EventHandler PresenterChanged;
        public event EventHandler Dismissed;
        public event EventHandler Recalculated;
        public event EventHandler<ValueChangedEventArgs<CompletionSet>> SelectedCompletionSetChanged;
        public event EventHandler Committed;

        public StubCompletionSession(ITextView textView, ObservableCollection<CompletionSet> completionSets)
        {
            TextView = textView;
            CompletionSets = new ReadOnlyObservableCollection<CompletionSet>(completionSets);
            IsStarted = true;
        }

        public ITrackingPoint GetTriggerPoint(ITextBuffer textBuffer)
        {
            throw new NotImplementedException();
        }

        public SnapshotPoint? GetTriggerPoint(ITextSnapshot textSnapshot)
        {
            return TextView.Caret.Position.BufferPosition;
        }

        public void Start()
        {
            throw new NotImplementedException();
        }

        public void Dismiss()
        {
            throw new NotImplementedException();
        }

        public void Recalculate()
        {
            throw new NotImplementedException();
        }

        public bool Match()
        {
            throw new NotImplementedException();
        }

        public void Collapse()
        {
            throw new NotImplementedException();
        }

        public void Filter()
        {
            throw new NotImplementedException();
        }

        public void Commit()
        {
            var completionSet = SelectedCompletionSet;
            if (completionSet == null)
                return;

            if (!completionSet.SelectionStatus.IsSelected || completionSet.SelectionStatus.Completion == null)
                return;

            var textBuffer = TextView.TextBuffer;
            using (var textEdit = textBuffer.CreateEdit())
            {
                textEdit.Replace(
                    completionSet.ApplicableTo.GetSpan(textBuffer.CurrentSnapshot), 
                    completionSet.SelectionStatus.Completion.InsertionText);
                textEdit.Apply();
            }
        }

    }
}