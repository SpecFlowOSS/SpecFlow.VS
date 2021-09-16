using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using SpecFlow.VisualStudio.Editor.Commands.Infrastructure;
using SpecFlow.VisualStudio.Editor.Services;
using SpecFlow.VisualStudio.ProjectSystem;
using SpecFlow.VisualStudio.VsxStubs.ProjectSystem;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.VisualStudio.Text.Projection;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;

namespace SpecFlow.VisualStudio.VsxStubs
{
    public class StubWpfTextView : IWpfTextView
    {
        class FilePathProvider : IVsTextBuffer, IPersistFileFormat
        {
            private readonly string _filePath;

            public FilePathProvider(string filePath)
            {
                _filePath = filePath;
            }

            #region IVsTextBuffer

            public int LockBuffer()
            {
                throw new NotImplementedException();
            }

            public int UnlockBuffer()
            {
                throw new NotImplementedException();
            }

            public int InitializeContent(string pszText, int iLength)
            {
                throw new NotImplementedException();
            }

            public int GetStateFlags(out uint pdwReadOnlyFlags)
            {
                throw new NotImplementedException();
            }

            public int SetStateFlags(uint dwReadOnlyFlags)
            {
                throw new NotImplementedException();
            }

            public int GetPositionOfLine(int iLine, out int piPosition)
            {
                throw new NotImplementedException();
            }

            public int GetPositionOfLineIndex(int iLine, int iIndex, out int piPosition)
            {
                throw new NotImplementedException();
            }

            public int GetLineIndexOfPosition(int iPosition, out int piLine, out int piColumn)
            {
                throw new NotImplementedException();
            }

            public int GetLengthOfLine(int iLine, out int piLength)
            {
                throw new NotImplementedException();
            }

            public int GetLineCount(out int piLineCount)
            {
                throw new NotImplementedException();
            }

            public int GetSize(out int piLength)
            {
                throw new NotImplementedException();
            }

            public int GetLanguageServiceID(out Guid pguidLangService)
            {
                throw new NotImplementedException();
            }

            public int SetLanguageServiceID(ref Guid guidLangService)
            {
                throw new NotImplementedException();
            }

            public int GetUndoManager(out IOleUndoManager ppUndoManager)
            {
                throw new NotImplementedException();
            }

            public int Reserved1()
            {
                throw new NotImplementedException();
            }

            public int Reserved2()
            {
                throw new NotImplementedException();
            }

            public int Reserved3()
            {
                throw new NotImplementedException();
            }

            public int Reserved4()
            {
                throw new NotImplementedException();
            }

            public int Reload(int fUndoable)
            {
                throw new NotImplementedException();
            }

            public int LockBufferEx(uint dwFlags)
            {
                throw new NotImplementedException();
            }

            public int UnlockBufferEx(uint dwFlags)
            {
                throw new NotImplementedException();
            }

            public int GetLastLineIndex(out int piLine, out int piIndex)
            {
                throw new NotImplementedException();
            }

            public int Reserved5()
            {
                throw new NotImplementedException();
            }

            public int Reserved6()
            {
                throw new NotImplementedException();
            }

            public int Reserved7()
            {
                throw new NotImplementedException();
            }

            public int Reserved8()
            {
                throw new NotImplementedException();
            }

            public int Reserved9()
            {
                throw new NotImplementedException();
            }

            public int Reserved10()
            {
                throw new NotImplementedException();
            }

            #endregion
            #region IPersistFileFormat

            int IPersist.GetClassID(out Guid pClassID)
            {
                throw new NotImplementedException();
            }

            int IPersistFileFormat.GetClassID(out Guid pClassID)
            {
                throw new NotImplementedException();
            }

            public int IsDirty(out int pfIsDirty)
            {
                throw new NotImplementedException();
            }

            public int InitNew(uint nFormatIndex)
            {
                throw new NotImplementedException();
            }

            public int Load(string pszFilename, uint grfMode, int fReadOnly)
            {
                throw new NotImplementedException();
            }

            public int Save(string pszFilename, int fRemember, uint nFormatIndex)
            {
                throw new NotImplementedException();
            }

            public int SaveCompleted(string pszFilename)
            {
                throw new NotImplementedException();
            }

            public int GetCurFile(out string ppszFilename, out uint pnFormatIndex)
            {
                ppszFilename = _filePath;
                pnFormatIndex = 0;
                return 0;
            }

            public int GetFormatList(out string ppszFormatList)
            {
                throw new NotImplementedException();
            }

            #endregion
        }

        public static StubWpfTextView CreateTextView(StubIdeScope ideScope, TestText inputText, string newLine = null, IProjectScope projectScope = null, string contentType = "deveroom", string filePath = null)
        {
            var textBuffer = VsxStubObjects.CreateTextBuffer(inputText.ToString(newLine));
            textBuffer.Properties.AddProperty(typeof(IProjectScope), projectScope);
            if (filePath != null)
                textBuffer.Properties.AddProperty(typeof(IVsTextBuffer), new FilePathProvider(filePath));

            var textView = new StubWpfTextView(textBuffer);
            if (contentType == "deveroom")
            {
                var tagAggregator = new StubBufferTagAggregatorFactoryService(ideScope).CreateTagAggregator<DeveroomTag>(textView.TextBuffer);
                tagAggregator.GetTags(new SnapshotSpan(textView.TextSnapshot, 0, textView.TextSnapshot.Length)).ToArray();
            }

            inputText.SetSelection(textView);
            inputText.SetCaret(textView);

            return textView;
        }

        private readonly ITextBuffer _textBuffer;
        private readonly StubTextCaret _caret;
        private readonly ITextSelection _selection;
        private readonly Dictionary<string, IAdornmentLayer> _adornmentLayers = new Dictionary<string, IAdornmentLayer>();
        private readonly StubEditorOptions _options = new StubEditorOptions();

        public StubEditorOptions StubEditorOptions => _options;

        public StubWpfTextView(ITextBuffer textBuffer)
        {
            _textBuffer = textBuffer;
            _caret = new StubTextCaret(this);
            //_selection = VsxStubObjects.CreateObject<ITextSelection>(
            //    "Microsoft.VisualStudio.Text.Editor.Implementation.WpfTextSelection, Microsoft.VisualStudio.Platform.VSEditor",
            //    this, new StubEditorFormatMap(), VsxStubObjects.GuardedOperations);
            _selection = new StubTextSelection(this);
        }

        public PropertyCollection Properties { get; } = new PropertyCollection();

        public void DisplayTextLineContainingBufferPosition(SnapshotPoint bufferPosition, double verticalDistance,
            ViewRelativePosition relativeTo)
        {
            throw new NotImplementedException();
        }

        public void DisplayTextLineContainingBufferPosition(SnapshotPoint bufferPosition, double verticalDistance,
            ViewRelativePosition relativeTo, double? viewportWidthOverride, double? viewportHeightOverride)
        {
            throw new NotImplementedException();
        }

        public SnapshotSpan GetTextElementSpan(SnapshotPoint point)
        {
            throw new NotImplementedException();
        }

        public void Close()
        {
            throw new NotImplementedException();
        }

        public void QueueSpaceReservationStackRefresh()
        {
            throw new NotImplementedException();
        }

        public IWpfTextViewLine GetTextViewLineContainingBufferPosition(SnapshotPoint bufferPosition)
        {
            throw new NotImplementedException();
        }

        public FrameworkElement VisualElement
        {
            get { throw new NotImplementedException(); }
        }

        public Brush Background
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public IAdornmentLayer GetAdornmentLayer(string name)
        {
            if (!_adornmentLayers.TryGetValue(name, out var adornmentLayer))
            {
                //adornmentLayer = VsxStubObjects.CreateObject<IAdornmentLayer>(
                //    "Microsoft.VisualStudio.Text.Editor.Implementation.AdornmentLayer, Microsoft.VisualStudio.Platform.VSEditor",
                //    this, name, false);
                adornmentLayer = new StubAdornmentLayer();
                _adornmentLayers[name] = adornmentLayer;
            }
            return adornmentLayer;
        }

        public ISpaceReservationManager GetSpaceReservationManager(string name)
        {
            throw new NotImplementedException();
        }

        ITextViewLine ITextView.GetTextViewLineContainingBufferPosition(SnapshotPoint bufferPosition)
        {
            return GetTextViewLineContainingBufferPosition(bufferPosition);
        }

        public bool InLayout => true; //in a process of layout

        public IViewScroller ViewScroller
        {
            get { throw new NotImplementedException(); }
        }

        public IWpfTextViewLineCollection TextViewLines
        {
            get { throw new NotImplementedException(); }
        }

        public IFormattedLineSource FormattedLineSource
        {
            get { throw new NotImplementedException(); }
        }

        public ILineTransformSource LineTransformSource
        {
            get { throw new NotImplementedException(); }
        }

        public double ZoomLevel
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public event EventHandler<BackgroundBrushChangedEventArgs> BackgroundBrushChanged;
        public event EventHandler<ZoomLevelChangedEventArgs> ZoomLevelChanged;
        ITextViewLineCollection ITextView.TextViewLines
        {
            get { return TextViewLines; }
        }

        public ITextCaret Caret => _caret;

        public ITextSelection Selection => _selection;

        public ITrackingSpan ProvisionalTextHighlight
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public ITextViewRoleSet Roles
        {
            get { throw new NotImplementedException(); }
        }

        public ITextBuffer TextBuffer => _textBuffer;

        public IBufferGraph BufferGraph
        {
            get { throw new NotImplementedException(); }
        }

        public ITextSnapshot TextSnapshot => _textBuffer.CurrentSnapshot;

        public ITextSnapshot VisualSnapshot
        {
            get { throw new NotImplementedException(); }
        }

        public ITextViewModel TextViewModel
        {
            get { throw new NotImplementedException(); }
        }

        public ITextDataModel TextDataModel
        {
            get { throw new NotImplementedException(); }
        }

        public double MaxTextRightCoordinate
        {
            get { throw new NotImplementedException(); }
        }

        public double ViewportLeft
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public double ViewportTop
        {
            get { throw new NotImplementedException(); }
        }

        public double ViewportRight
        {
            get { throw new NotImplementedException(); }
        }

        public double ViewportBottom
        {
            get { throw new NotImplementedException(); }
        }

        public double ViewportWidth
        {
            get { throw new NotImplementedException(); }
        }

        public double ViewportHeight
        {
            get { throw new NotImplementedException(); }
        }

        public double LineHeight
        {
            get { throw new NotImplementedException(); }
        }

        public bool IsClosed { get; } = false; // Simulate view to be open

        public IEditorOptions Options => _options;

        public bool IsMouseOverViewOrAdornments
        {
            get { throw new NotImplementedException(); }
        }

        public bool HasAggregateFocus { get; } = true; // Simulate view to be in focus

        public event EventHandler<TextViewLayoutChangedEventArgs> LayoutChanged;
        public event EventHandler ViewportLeftChanged;
        public event EventHandler ViewportHeightChanged;
        public event EventHandler ViewportWidthChanged;
        public event EventHandler<MouseHoverEventArgs> MouseHover;
        public event EventHandler Closed;
        public event EventHandler LostAggregateFocus;
        public event EventHandler GotAggregateFocus;

        public Tuple<int,int> GetCaretPosition()
        {
            var pos = Caret.Position.BufferPosition;
            var line = pos.GetContainingLine();
            return new Tuple<int, int>(line.LineNumber, pos.Position - line.Start.Position);
        }

        public void SimulateTypeText(DeveroomEditorTypeCharCommandBase command, string text)
        {
            foreach (var ch in text)
            {
                SimulateType(command, ch);
            }
        }

        public void SimulateType(DeveroomEditorTypeCharCommandBase command, char c)
        {
            var caretPosition = Caret.Position.BufferPosition.Position;
            using (var textEdit = TextBuffer.CreateEdit())
            {
                textEdit.Insert(Caret.Position.BufferPosition.Position, c.ToString());
                textEdit.Apply();
            }
            Caret.MoveTo(new SnapshotPoint(TextSnapshot, caretPosition + 1));
            ForceReparse(); //this is needed because currently partial table formatting is not supported

            command.PostExec(this, c);
        }

        public void ForceReparse()
        {
            var tagger = DeveroomTaggerProvider.GetDeveroomTagger(TextBuffer);
            tagger?.GetTags(
                new NormalizedSnapshotSpanCollection(new[]
                    {new SnapshotSpan(TextSnapshot, 0, TextSnapshot.Length)}), true).ToArray();
        }

    }
}
