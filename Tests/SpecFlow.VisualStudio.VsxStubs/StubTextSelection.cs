using System;
using System.Collections.ObjectModel;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;

namespace SpecFlow.VisualStudio.VsxStubs
{
    public class StubTextSelection : ITextSelection
    {
        private readonly StubWpfTextView _wpfTextView;
        private VirtualSnapshotPoint _anchorPoint;
        private VirtualSnapshotPoint _activePoint;
        private double _leftX;
        private double _rightX;
        private TextSelectionMode _selectionMode;
        private bool _activationTracksFocus;
        private bool _isActive;

        public StubTextSelection(StubWpfTextView textView)
        {
            _wpfTextView = textView;

            this._activePoint = this._anchorPoint = new VirtualSnapshotPoint(this._wpfTextView.TextSnapshot, 0);
            this._selectionMode = TextSelectionMode.Stream;
        }

        public void Select(SnapshotSpan selectionSpan, bool isReversed)
        {
            VirtualSnapshotPoint virtualSnapshotPoint1 = new VirtualSnapshotPoint(selectionSpan.Start);
            VirtualSnapshotPoint virtualSnapshotPoint2 = new VirtualSnapshotPoint(selectionSpan.End);
            if (isReversed)
                this.Select(virtualSnapshotPoint2, virtualSnapshotPoint1);
            else
                this.Select(virtualSnapshotPoint1, virtualSnapshotPoint2);
        }

        public void Select(VirtualSnapshotPoint anchorPoint, VirtualSnapshotPoint activePoint)
        {
            if (anchorPoint.Position.Snapshot != this._wpfTextView.TextSnapshot)
                throw new ArgumentException(nameof(anchorPoint));
            if (activePoint.Position.Snapshot != this._wpfTextView.TextSnapshot)
                throw new ArgumentException(nameof(activePoint));
            if (anchorPoint == activePoint)
            {
                this.Clear(false);
            }
            else
            {
                VirtualSnapshotPoint anchorPoint1 = this.NormalizePoint(anchorPoint);
                VirtualSnapshotPoint activePoint1 = this.NormalizePoint(activePoint);
                if (anchorPoint1 == activePoint1)
                    this.Clear(false);
                else
                    this.InnerSelect(anchorPoint1, activePoint1);
            }
        }

        private VirtualSnapshotPoint NormalizePoint(VirtualSnapshotPoint point)
        {
            return point;
            //TODO: more accurate stub?
            //ITextViewLine containingBufferPosition = (ITextViewLine)this._wpfTextView.GetTextViewLineContainingBufferPosition(point.Position);
            //if ((int)point.Position >= (int)containingBufferPosition.End)
            //    return new VirtualSnapshotPoint(containingBufferPosition.End, point.VirtualSpaces);
            //return new VirtualSnapshotPoint(containingBufferPosition.GetTextElementSpan(point.Position).Start);
        }

        private void InnerSelect(VirtualSnapshotPoint anchorPoint, VirtualSnapshotPoint activePoint)
        {
            bool isEmpty = this.IsEmpty;
            this.ActivationTracksFocus = true;
            this._anchorPoint = anchorPoint;
            this._activePoint = activePoint;
            VirtualSnapshotPoint bufferPosition1 = this._anchorPoint;
            VirtualSnapshotPoint bufferPosition2 = this._activePoint;
            if (this._anchorPoint > this._activePoint)
            {
                bufferPosition1 = this._activePoint;
                bufferPosition2 = this._anchorPoint;
            }
            if (this.Mode == TextSelectionMode.Box)
            {
                IWpfTextViewLine containingBufferPosition1 = this._wpfTextView.GetTextViewLineContainingBufferPosition(bufferPosition1.Position);
                IWpfTextViewLine containingBufferPosition2 = this._wpfTextView.GetTextViewLineContainingBufferPosition(bufferPosition2.Position);
                TextBounds extendedCharacterBounds = containingBufferPosition1.GetExtendedCharacterBounds(bufferPosition1);
                this._leftX = extendedCharacterBounds.Leading;
                extendedCharacterBounds = containingBufferPosition2.GetExtendedCharacterBounds(bufferPosition2);
                this._rightX = extendedCharacterBounds.Leading;
                if (this._rightX < this._leftX)
                {
                    double leftX = this._leftX;
                    this._leftX = this._rightX;
                    this._rightX = leftX;
                }
            }
            //TODO: this.RaiseChangedEvent(isEmpty, this.IsEmpty, true);
        }

        private void Clear(bool resetMode)
        {
            bool isEmpty = this.IsEmpty;
            this._anchorPoint = this._activePoint;
            this.ActivationTracksFocus = true;
            if (resetMode)
                this.Mode = TextSelectionMode.Stream;
            //TODO this.RaiseChangedEvent(isEmpty, true, false);
        }

        public void Clear()
        {
            this.Clear(true);
        }

        public VirtualSnapshotSpan? GetSelectionOnTextViewLine(ITextViewLine line)
        {
            throw new NotImplementedException();
        }

        public ITextView TextView
        {
            get { throw new NotImplementedException(); }
        }

        public NormalizedSnapshotSpanCollection SelectedSpans
        {
            get { throw new NotImplementedException(); }
        }

        public ReadOnlyCollection<VirtualSnapshotSpan> VirtualSelectedSpans
        {
            get { throw new NotImplementedException(); }
        }

        public VirtualSnapshotSpan StreamSelectionSpan
        {
            get { throw new NotImplementedException(); }
        }

        public TextSelectionMode Mode
        {
            get
            {
                return this._selectionMode;
            }
            set
            {
                if (this._selectionMode == value)
                    return;
                this._selectionMode = value;
                if (this.IsEmpty)
                    return;
                this.Select(this.AnchorPoint, this.ActivePoint);
            }
        }
        public bool IsReversed
        {
            get
            {
                return this._activePoint < this._anchorPoint;
            }
        }

        public bool IsEmpty
        {
            get
            {
                return this._activePoint == this._anchorPoint;
            }
        }

        public bool IsActive
        {
            set
            {
                this._isActive = value;
            }
            get
            {
                return this._isActive;
            }
        }
        public bool ActivationTracksFocus
        {
            set
            {
                if (this._activationTracksFocus == value)
                    return;
                this._activationTracksFocus = value;
                if (!this._activationTracksFocus)
                    return;
                this.IsActive = this._wpfTextView.HasAggregateFocus;
            }
            get
            {
                return this._activationTracksFocus;
            }
        }
        public VirtualSnapshotPoint ActivePoint
        {
            get
            {
                if (!this.IsEmpty)
                    return this._activePoint;
                return this._wpfTextView.Caret.Position.VirtualBufferPosition;
            }
        }

        public VirtualSnapshotPoint AnchorPoint
        {
            get
            {
                if (!this.IsEmpty)
                    return this._anchorPoint;
                return this._wpfTextView.Caret.Position.VirtualBufferPosition;
            }
        }
        public VirtualSnapshotPoint Start
        {
            get
            {
                if (!this.IsReversed)
                    return this.AnchorPoint;
                return this.ActivePoint;
            }
        }

        public VirtualSnapshotPoint End
        {
            get
            {
                if (!this.IsReversed)
                    return this.ActivePoint;
                return this.AnchorPoint;
            }
        }

        public event EventHandler SelectionChanged;
    }
}
