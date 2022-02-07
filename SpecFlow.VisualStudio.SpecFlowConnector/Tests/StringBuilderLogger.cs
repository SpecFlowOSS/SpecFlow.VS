namespace SpecFlowConnector.Tests;

public class StringBuilderLogger : Logger
{
    private readonly StringWriter _writer;

    public StringBuilderLogger()
    {
        _writer = new StringWriter();
    }

    protected override string Format(Log log) => $"{log.Level} {log.Message}";

    protected override TextWriter GetTextWriter(LogLevel level) => _writer;

    public override string ToString() => _writer.GetStringBuilder().ToString();
}
