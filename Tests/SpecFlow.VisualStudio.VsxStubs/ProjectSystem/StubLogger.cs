﻿using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Linq.Expressions;
using System.Text;
using Microsoft.VisualStudio.Telemetry;

namespace SpecFlow.VisualStudio.VsxStubs.ProjectSystem;

public record LogMessage(TraceLevel Level, string Message, int Order, TimeSpan TimeStamp);

public class StubLogger : IDeveroomLogger
{
    private volatile int _order;
    private readonly Stopwatch _stopwatch;

    public StubLogger()
    {
        _stopwatch = Stopwatch.StartNew();
    }

    private StubLogger(Stopwatch stopwatch, IEnumerable<LogMessage> messages)
    {
        _stopwatch = stopwatch;
        Logs = new ConcurrentBag<LogMessage>(messages);
    }

    public ConcurrentBag<LogMessage> Logs { get; } = new ();
    public ImmutableArray<string> Messages => Logs.Select(l => l.Message).ToImmutableArray();

    public TraceLevel Level => TraceLevel.Verbose;

    public void Log(TraceLevel messageLevel, string message)
    {
        Logs.Add(new LogMessage(messageLevel, message, Interlocked.Increment(ref _order), _stopwatch.Elapsed));
    }

    public StubLogger Errors() => WithLevel(level => level == TraceLevel.Error);
    public StubLogger Warnings() => WithLevel(level => level == TraceLevel.Warning);

    public StubLogger WithLevel(Func<TraceLevel, bool> predicate)
    {
        return new StubLogger(
            _stopwatch, Logs.Where(m => predicate(m.Level)));
    }

    public StubLogger WithoutHeader(string warningHeader)
    {
        return new StubLogger(
            _stopwatch, Logs.Select(m => new LogMessage(m.Level, m.Message.Replace(warningHeader, string.Empty), m.Order, m.TimeStamp)));
    }
}
