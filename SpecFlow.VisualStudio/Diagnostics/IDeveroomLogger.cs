namespace SpecFlow.VisualStudio.Diagnostics;

public interface IDeveroomLogger
{
    TraceLevel Level { get; }
    void Log(LogMessage message);
}
