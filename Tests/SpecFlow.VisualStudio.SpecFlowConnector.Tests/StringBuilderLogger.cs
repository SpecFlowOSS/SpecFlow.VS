namespace SpecFlow.VisualStudio.SpecFlowConnector.Tests;

public class StringBuilderLogger : Logger
{
    private readonly ConcurrentDictionary<LogLevel, StringWriter> _builders;

    public StringBuilderLogger()
    {
        _builders = new ConcurrentDictionary<LogLevel, StringWriter>();
    }

    public string this[LogLevel level]
    {
        get
        {
            var stringWriter = GetTextWriter(level) as StringWriter;
            return stringWriter!.GetStringBuilder().ToString().TrimEnd('\r','\n');
        }
    }

    protected override string Format(Log log) => log.Message;

    protected override TextWriter GetTextWriter(LogLevel level) => _builders.GetOrAdd(level, _ => new StringWriter());
}
