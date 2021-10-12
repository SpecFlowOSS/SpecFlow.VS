#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SpecFlow.VisualStudio.Analytics;

namespace SpecFlow.VisualStudio.VsxStubs.ProjectSystem
{
    public class StubAnalyticsTransmitter : IAnalyticsTransmitter
    {
        private ConcurrentBag<IAnalyticsEvent> Events { get; } =new ();
        private TaskCompletionSource<IAnalyticsEvent> _transmitSignal = new();

        public void TransmitEvent(IAnalyticsEvent runtimeEvent)
        {
            Events.Add(runtimeEvent);
            var signal = Interlocked.Exchange(ref _transmitSignal, new TaskCompletionSource<IAnalyticsEvent>());
            signal.SetResult(runtimeEvent);
        }

        public void TransmitExceptionEvent(Exception exception, Dictionary<string, object> additionalProps = null, bool? isFatal = null,
            bool anonymize = true)
        {
           //nop
        }

        public Task<IAnalyticsEvent> WaitForEventAsync(string eventName)
        {
            CancellationTokenSource cts = Debugger.IsAttached
                ? new CancellationTokenSource(TimeSpan.FromMinutes(1))
                : new CancellationTokenSource(TimeSpan.FromSeconds(2));
            return WaitForEventAsync(eventName, cts.Token);
        }

        public async Task<IAnalyticsEvent> WaitForEventAsync(string eventName, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var analyticsEvent = Events.FirstOrDefault(ev => ev.EventName == eventName);
                if (analyticsEvent != null) return analyticsEvent;

                await Task.WhenAny(_transmitSignal.Task, Task.Delay(-1, cancellationToken));
            }

            throw new TaskCanceledException();
        }
    }
}
