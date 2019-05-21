using System;
using System.Runtime.InteropServices;
using Deveroom.VisualStudio.Monitoring;
using Deveroom.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;

namespace Deveroom.VisualStudio.Editor.Commands.Infrastructure
{
    public abstract class DeveroomEditorTypeCharCommandBase : DeveroomEditorCommandBase
    {
        public override DeveroomEditorCommandTargetKey Target 
            => new DeveroomEditorCommandTargetKey(VSConstants.VSStd2K, VSConstants.VSStd2KCmdID.TYPECHAR);

        protected DeveroomEditorTypeCharCommandBase(IIdeScope ideScope, IBufferTagAggregatorFactoryService aggregatorFactory, IMonitoringService monitoringService) : base(ideScope, aggregatorFactory, monitoringService)
        {
        }

        private char GetTypeChar(IntPtr inArgs)
        {
            return (char)(ushort)Marshal.GetObjectForNativeVariant(inArgs);
        }

        public override bool PostExec(IWpfTextView textView, DeveroomEditorCommandTargetKey commandKey, IntPtr inArgs = default(IntPtr))
        {
            char ch = GetTypeChar(inArgs);
            return PostExec(textView, ch);
        }

        protected internal abstract bool PostExec(IWpfTextView textView, char ch);
    }
}