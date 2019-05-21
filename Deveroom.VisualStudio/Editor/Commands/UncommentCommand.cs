using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using Deveroom.VisualStudio.Editor.Commands.Infrastructure;
using Deveroom.VisualStudio.Monitoring;
using Deveroom.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;

namespace Deveroom.VisualStudio.Editor.Commands
{
    [Export(typeof(IDeveroomFeatureEditorCommand))]
    public class UncommentCommand : DeveroomEditorCommandBase, IDeveroomFeatureEditorCommand
    {
        public override DeveroomEditorCommandTargetKey[] Targets => new[]
        {
            new DeveroomEditorCommandTargetKey(VSConstants.VSStd2K, VSConstants.VSStd2KCmdID.UNCOMMENTBLOCK),
            new DeveroomEditorCommandTargetKey(VSConstants.VSStd2K, VSConstants.VSStd2KCmdID.UNCOMMENT_BLOCK)
        };

        [ImportingConstructor]
        public UncommentCommand(IIdeScope ideScope, IBufferTagAggregatorFactoryService aggregatorFactory, IMonitoringService monitoringService) : base(ideScope, aggregatorFactory, monitoringService)
        {
        }

        public override bool PreExec(IWpfTextView textView, DeveroomEditorCommandTargetKey commandKey, IntPtr inArgs = default(IntPtr))
        {
            MonitoringService.MonitorCommandCommentUncomment();

            var selectionSpan = GetSelectionSpan(textView);
            var lines = GetSpanFullLines(selectionSpan).ToArray();
            Debug.Assert(lines.Length > 0);

            using (var textEdit = selectionSpan.Snapshot.TextBuffer.CreateEdit())
            {
                foreach (var line in lines)
                {
                    int commentCharPosition = line.GetText().IndexOf('#');
                    if (commentCharPosition >= 0)
                        textEdit.Delete(line.Start.Position + commentCharPosition, 1);
                }
                textEdit.Apply();
            }

            SetSelectionToChangedLines(textView, lines);

            return true;
        }
    }
}