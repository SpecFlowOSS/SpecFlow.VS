namespace SpecFlowConnector.Logging;

public abstract class Logger<T> : ILogger where T : TextWriter
{
    public void Log(Log log)
    {
        GetTextWriter(log.Level)
            .WriteLine(Format(log));
    }

    protected abstract string Format(Log log);

    protected abstract T GetTextWriter(LogLevel level);
}
