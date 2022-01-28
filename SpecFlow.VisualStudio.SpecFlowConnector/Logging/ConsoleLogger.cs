// ReSharper disable once CheckNamespace

namespace SpecFlow.VisualStudio.SpecFlowConnector;

public sealed class ConsoleLogger : Logger
{
    public ConsoleLogger()
    {
        Console.OutputEncoding = Encoding.UTF8;
    }

    protected override string Format(Log log) => log.Message;

    protected override TextWriter GetTextWriter(LogLevel level)
    {
        switch (level)
        {
            case LogLevel.Error:
            case LogLevel.Warning:
                return Console.Error;
            case LogLevel.Info:
            case LogLevel.Verbose:
                return Console.Out;
            default:
                throw new ArgumentOutOfRangeException(nameof(level), level, null);
        }
    }
}
