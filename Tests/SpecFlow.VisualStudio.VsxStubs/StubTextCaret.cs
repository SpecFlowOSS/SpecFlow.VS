using System;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;

namespace SpecFlow.VisualStudio.VsxStubs
{
    public class StubTextCaret : ITextCaret
    {
        private readonly StubWpfTextView _wpfTextView;
        private ITextBuffer _textBuffer;
        private VirtualSnapshotPoint _insertionPoint;
        private PositionAffinity _caretAffinity;

        public StubTextCaret(StubWpfTextView wpfTextView)
        {
            _wpfTextView = wpfTextView;
            this._textBuffer = wpfTextView.TextBuffer;
            this._insertionPoint = new VirtualSnapshotPoint(new SnapshotPoint(_textBuffer.CurrentSnapshot, 0));
            this._caretAffinity = PositionAffinity.Successor;
        }

        public void EnsureVisible()
        {
            throw new NotImplementedException();
        }

        public CaretPosition MoveTo(ITextViewLine textLine, double xCoordinate)
        {
            throw new NotImplementedException();
        }

        public CaretPosition MoveTo(ITextViewLine textLine, double xCoordinate, bool captureHorizontalPosition)
        {
            throw new NotImplementedException();
        }

        public CaretPosition MoveTo(ITextViewLine textLine)
        {
            throw new NotImplementedException();
        }

        public CaretPosition MoveTo(VirtualSnapshotPoint bufferPosition)
        {
            this.InternalMoveTo(bufferPosition, PositionAffinity.Successor, true, true, true);
            return this.Position;
        }

        public CaretPosition MoveTo(VirtualSnapshotPoint bufferPosition, PositionAffinity caretAffinity)
        {
            this.InternalMoveTo(bufferPosition, caretAffinity, true, true, true);
            return this.Position;
        }

        public CaretPosition MoveTo(VirtualSnapshotPoint bufferPosition, PositionAffinity caretAffinity, bool captureHorizontalPosition)
        {
            this.InternalMoveTo(bufferPosition, caretAffinity, captureHorizontalPosition, true, true);
            return this.Position;
        }

        public CaretPosition MoveTo(SnapshotPoint bufferPosition)
        {
            this.InternalMoveTo(new VirtualSnapshotPoint(bufferPosition), PositionAffinity.Successor, true, true, true);
            return this.Position;
        }

        public CaretPosition MoveTo(SnapshotPoint bufferPosition, PositionAffinity caretAffinity)
        {
            this.InternalMoveTo(new VirtualSnapshotPoint(bufferPosition), caretAffinity, true, true, true);
            return this.Position;
        }

        public CaretPosition MoveTo(SnapshotPoint bufferPosition, PositionAffinity caretAffinity, bool captureHorizontalPosition)
        {
            this.InternalMoveTo(new VirtualSnapshotPoint(bufferPosition), caretAffinity, captureHorizontalPosition, true, true);
            return this.Position;
        }

        private void InternalMoveTo(VirtualSnapshotPoint bufferPosition, PositionAffinity caretAffinity, bool captureHorizontalPosition, bool captureVerticalPosition, bool raiseEvent)
        {
            if (bufferPosition.Position.Snapshot != this._wpfTextView.TextSnapshot)
                throw new ArgumentException(nameof(bufferPosition));
            _insertionPoint = bufferPosition;
            _caretAffinity = caretAffinity;

            //ITextViewLine containingTextViewLine = this.GetContainingTextViewLine(bufferPosition.Position, caretAffinity);
            //VirtualSnapshotPoint bufferPosition1 = CaretElement.NormalizePosition(bufferPosition, containingTextViewLine);
            //if (bufferPosition1 != bufferPosition)
            //    raiseEvent = true;
            //this.InternalMoveCaret(bufferPosition1, caretAffinity, containingTextViewLine, captureHorizontalPosition, captureVerticalPosition, raiseEvent);
        }


        public CaretPosition MoveToPreferredCoordinates()
        {
            throw new NotImplementedException();
        }

        public CaretPosition MoveToNextCaretPosition()
        {
            throw new NotImplementedException();
        }

        public CaretPosition MoveToPreviousCaretPosition()
        {
            throw new NotImplementedException();
        }

        public ITextViewLine ContainingTextViewLine
        {
            get { throw new NotImplementedException(); }
        }

        public double Left
        {
            get { throw new NotImplementedException(); }
        }

        public double Width
        {
            get { throw new NotImplementedException(); }
        }

        public double Right
        {
            get { throw new NotImplementedException(); }
        }

        public double Top
        {
            get { throw new NotImplementedException(); }
        }

        public double Height
        {
            get { throw new NotImplementedException(); }
        }

        public double Bottom
        {
            get { throw new NotImplementedException(); }
        }

        public CaretPosition Position
        {
            get
            {
                return new CaretPosition(this._insertionPoint, VsxStubObjects.BufferGraphFactoryService.CreateBufferGraph(_textBuffer).CreateMappingPoint(this._insertionPoint.Position, PointTrackingMode.Positive), this._caretAffinity);
            }
        }
        public bool OverwriteMode
        {
            get { throw new NotImplementedException(); }
        }

        public bool InVirtualSpace
        {
            get { throw new NotImplementedException(); }
        }

        public bool IsHidden
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public event EventHandler<CaretPositionChangedEventArgs> PositionChanged;
    }
}
