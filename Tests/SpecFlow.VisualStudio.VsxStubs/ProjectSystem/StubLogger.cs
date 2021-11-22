namespace SpecFlow.VisualStudio.VsxStubs.ProjectSystem;

public record LogMessage(TraceLevel Level, string Message);

public class StubLogger : IDeveroomLogger
{
    public List<LogMessage> Messages { get; } = new ();
    public TraceLevel Level => TraceLevel.Verbose;

    public void Log(TraceLevel messageLevel, string message)
    {
        Messages.Add(new (messageLevel, message));
    }
}

