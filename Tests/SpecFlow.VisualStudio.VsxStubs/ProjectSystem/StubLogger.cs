#nullable enable

namespace SpecFlow.VisualStudio.VsxStubs.ProjectSystem;

public record LogMessage(TraceLevel Level, string Message, int Order, TimeSpan TimeStamp, string CallerMethod);

public class StubLogger : IDeveroomLogger
{
    private readonly Stopwatch _stopwatch;
    private volatile int _order;

    public StubLogger()
    {
        _stopwatch = Stopwatch.StartNew();
    }

    private StubLogger(Stopwatch stopwatch, IEnumerable<LogMessage> messages)
    {
        _stopwatch = stopwatch;
        Logs = new ConcurrentBag<LogMessage>(messages);
    }

    public ConcurrentBag<LogMessage> Logs { get; private set; } = new();
    public ImmutableArray<string> Messages => Logs.Select(l => l.Message).ToImmutableArray();

    public TraceLevel Level => TraceLevel.Verbose;

    public void Log(TraceLevel messageLevel, string message)
    {
        var callerMethodAndMessage = message.Split(new[] {':'}, 2);
        Logs.Add(new LogMessage(messageLevel, callerMethodAndMessage[1].Trim(), Interlocked.Increment(ref _order),
            _stopwatch.Elapsed, callerMethodAndMessage[0]));
    }

    public StubLogger Errors()
    {
        return WithLevel(level => level == TraceLevel.Error);
    }

    public StubLogger Warnings()
    {
        return WithLevel(level => level == TraceLevel.Warning);
    }

    public StubLogger WithLevel(Func<TraceLevel, bool> predicate)
    {
        return new StubLogger(
            _stopwatch, Logs.Where(m => predicate(m.Level)));
    }

    public StubLogger WithoutHeader(string warningHeader)
    {
        return new StubLogger(
            _stopwatch,
            Logs.Select(m =>
                new LogMessage(m.Level, m.Message.Replace(warningHeader, string.Empty), m.Order, m.TimeStamp,
                    m.CallerMethod)));
    }

    public void Clear()
    {
        Logs = new ConcurrentBag<LogMessage>();
    }
}
