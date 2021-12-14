namespace SpecFlow.VisualStudio.VsxStubs.ProjectSystem;

public class DeveroomXUnitLogger : IDeveroomLogger
{
    private readonly Stopwatch _stopwatch;
    private readonly ITestOutputHelper _testOutputHelper;
    private volatile int _order;

    public DeveroomXUnitLogger(ITestOutputHelper testOutputHelper, TraceLevel level = TraceLevel.Verbose)
    {
        Level = level;
        _testOutputHelper = testOutputHelper;
        _stopwatch = Stopwatch.StartNew();
    }

    public TraceLevel Level { get; }

    public void Log(TraceLevel messageLevel, string message)
    {
        if (messageLevel <= Level)
            _testOutputHelper.WriteLine(
                $"{Interlocked.Increment(ref _order):0000} {_stopwatch.Elapsed:m\\:ss\\.ffffff} {messageLevel,5} {Thread.CurrentThread.ManagedThreadId,5} {message}");
    }
}
