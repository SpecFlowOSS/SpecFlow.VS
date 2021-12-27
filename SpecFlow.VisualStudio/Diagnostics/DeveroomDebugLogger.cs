namespace SpecFlow.VisualStudio.Diagnostics;

public class DeveroomDebugLogger : IDeveroomLogger
{
#if DEBUG
    private const TraceLevel DefaultDebugTraceLevel = TraceLevel.Verbose;
#else
    private const TraceLevel DefaultDebugTraceLevel = TraceLevel.Off;
#endif
    public TraceLevel Level { get; }

    public DeveroomDebugLogger(TraceLevel level = DefaultDebugTraceLevel)
    {
        Level = level;
        var env = Environment.GetEnvironmentVariable("SPECFLOWVS_DEBUG");
        if (env != null)
        {
            if (env.Equals("1") || env.Equals("true", StringComparison.InvariantCultureIgnoreCase))
                Level = TraceLevel.Verbose;
            else if (Enum.TryParse<TraceLevel>(env, true, out var envLevel)) Level = envLevel;
        }
    }

    public void Log(LogMessage message)
    {
        Debug.WriteLineIf(message.Level <= Level, $"{message.Level}: {message.CallerMethod}:{message.Message}",
            "SpecFlowVs");
    }
}
