﻿namespace SpecFlowConnector.Logging;

public abstract class Logger : ILogger
{
    public void Log(Log log)
    {
        GetTextWriter(log.Level)
            .WriteLine(Format(log));
    }

    protected abstract string Format(Log log);

    protected abstract TextWriter GetTextWriter(LogLevel level);
}
