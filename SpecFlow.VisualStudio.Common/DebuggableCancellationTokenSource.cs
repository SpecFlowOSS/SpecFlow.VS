namespace SpecFlow.VisualStudio.Common;

public class DebuggableCancellationTokenSource : CancellationTokenSource
{
    public DebuggableCancellationTokenSource(TimeSpan nonDebuggerTimeout)
        : base(Debugger.IsAttached ? TimeSpan.FromMinutes(1) : nonDebuggerTimeout)
    {
    }
}
