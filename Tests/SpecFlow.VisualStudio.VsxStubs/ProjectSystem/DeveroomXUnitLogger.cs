using System.Diagnostics;
using SpecFlow.VisualStudio.Diagnostics;
using Xunit.Abstractions;

namespace SpecFlow.VisualStudio.VsxStubs.ProjectSystem
{
    public class DeveroomXUnitLogger : IDeveroomLogger
    {
        private readonly ITestOutputHelper _testOutputHelper;
        public TraceLevel Level { get; }
        private volatile int _order;
        private readonly Stopwatch _stopwatch;

        public DeveroomXUnitLogger(ITestOutputHelper testOutputHelper, TraceLevel level = TraceLevel.Verbose)
        {
            Level = level;
            _testOutputHelper = testOutputHelper;
            _stopwatch = Stopwatch.StartNew();
        }

        public void Log(TraceLevel messageLevel, string message)
        {
            if (messageLevel <= Level)
            {
                _testOutputHelper.WriteLine($"{Interlocked.Increment(ref _order):0000} {_stopwatch.Elapsed} {messageLevel,7} {message}");
            }
        }
    }
}
