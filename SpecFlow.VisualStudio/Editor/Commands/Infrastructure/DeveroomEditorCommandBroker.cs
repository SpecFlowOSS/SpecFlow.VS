using Microsoft.VisualStudio.OLE.Interop;

namespace SpecFlow.VisualStudio.Editor.Commands.Infrastructure;

[Export(typeof(IVsTextViewCreationListener))]
[ContentType(VsContentTypes.FeatureFile)]
[TextViewRole(PredefinedTextViewRoles.Interactive)]
public class DeveroomFeatureEditorCommandBroker : DeveroomEditorCommandBroker<IDeveroomFeatureEditorCommand>
{
    [ImportingConstructor]
    public DeveroomFeatureEditorCommandBroker(IVsEditorAdaptersFactoryService adaptersFactory,
        [ImportMany] IEnumerable<IDeveroomFeatureEditorCommand> commands)
        : base(adaptersFactory, commands)
    {
    }
}

[Export(typeof(IVsTextViewCreationListener))]
[ContentType(VsContentTypes.CSharp)]
[TextViewRole(PredefinedTextViewRoles.Interactive)]
public class DeveroomCodeEditorCommandBroker : DeveroomEditorCommandBroker<IDeveroomCodeEditorCommand>
{
    [ImportingConstructor]
    public DeveroomCodeEditorCommandBroker(IVsEditorAdaptersFactoryService adaptersFactory,
        [ImportMany] IEnumerable<IDeveroomCodeEditorCommand> commands)
        : base(adaptersFactory, commands)
    {
    }
}

public abstract class DeveroomEditorCommandBroker<TCommand> : IVsTextViewCreationListener
    where TCommand : IDeveroomEditorCommand
{
    private readonly IVsEditorAdaptersFactoryService _adaptersFactory;
    private readonly List<TCommand> _commands;
    private readonly Lazy<Dictionary<DeveroomEditorCommandTargetKey, IDeveroomEditorCommand[]>> _editorCommandRegistry;

    protected DeveroomEditorCommandBroker(IVsEditorAdaptersFactoryService adaptersFactory,
        [ImportMany] IEnumerable<TCommand> commands)
    {
        _adaptersFactory = adaptersFactory;
        _commands = commands.ToList();
        _editorCommandRegistry =
            new Lazy<Dictionary<DeveroomEditorCommandTargetKey, IDeveroomEditorCommand[]>>(BuildRegistry,
                LazyThreadSafetyMode.ExecutionAndPublication);
    }

    public void VsTextViewCreated(IVsTextView textViewAdapter)
    {
        IWpfTextView view = _adaptersFactory.GetWpfTextView(textViewAdapter);
        Debug.Assert(view != null);

        var filter = new EditorCommandsFilter(view, _editorCommandRegistry.Value);

        textViewAdapter.AddCommandFilter(filter, out var next);
        filter.Next = next;
    }

    private Dictionary<DeveroomEditorCommandTargetKey, IDeveroomEditorCommand[]> BuildRegistry()
    {
        var list = new List<KeyValuePair<DeveroomEditorCommandTargetKey, IDeveroomEditorCommand>>();
        foreach (var editorCommand in _commands)
            list.AddRange(editorCommand.Targets.Select(target =>
                new KeyValuePair<DeveroomEditorCommandTargetKey, IDeveroomEditorCommand>(
                    new DeveroomEditorCommandTargetKey(target.CommandGroup, target.CommandId), editorCommand)));
        return list.GroupBy(item => item.Key).ToDictionary(g => g.Key, g => g.Select(item => item.Value).ToArray());
    }

    #region Command Filter

    private class EditorCommandsFilter : IOleCommandTarget
    {
        public EditorCommandsFilter(IWpfTextView textView,
            Dictionary<DeveroomEditorCommandTargetKey, IDeveroomEditorCommand[]> commandRegistry)
        {
            TextView = textView;
            CommandRegistry = commandRegistry;
        }

        private IWpfTextView TextView { get; }
        private Dictionary<DeveroomEditorCommandTargetKey, IDeveroomEditorCommand[]> CommandRegistry { get; }
        public IOleCommandTarget Next { get; set; }

        public int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
        {
            var commandKey = new DeveroomEditorCommandTargetKey(pguidCmdGroup, prgCmds[0].cmdID);
            if (CommandRegistry.TryGetValue(commandKey, out var commands))
                foreach (var editorCommand in commands)
                {
                    var status = editorCommand.QueryStatus(TextView, commandKey);
                    if (status != DeveroomEditorCommandStatus.NotSupported)
                    {
                        prgCmds[0].cmdf = (uint) OLECMDF.OLECMDF_SUPPORTED;
                        if (status == DeveroomEditorCommandStatus.Supported)
                            prgCmds[0].cmdf |= (uint) OLECMDF.OLECMDF_ENABLED;
                        return VSConstants.S_OK;
                    }
                }

            return Next.QueryStatus(pguidCmdGroup, cCmds, prgCmds, pCmdText);
        }

        public int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            bool handled = false;
            int hresult = VSConstants.S_OK;

            var commandKey = new DeveroomEditorCommandTargetKey(pguidCmdGroup, nCmdID);
            if (!CommandRegistry.TryGetValue(commandKey, out var commands))
                return Next.Exec(commandKey.CommandGroup, commandKey.CommandId, nCmdexecopt, pvaIn, pvaOut);

            // Pre-process
            foreach (var editorCommand in commands)
            {
                handled = editorCommand.PreExec(TextView, commandKey, pvaIn);
                if (handled)
                    break;
            }

            if (!handled)
                hresult = Next.Exec(commandKey.CommandGroup, commandKey.CommandId, nCmdexecopt, pvaIn, pvaOut);

            // Post-process
            foreach (var editorCommand in commands) editorCommand.PostExec(TextView, commandKey, pvaIn);

            return hresult;
        }
    }

    #endregion
}
