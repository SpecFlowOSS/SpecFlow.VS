using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SpecFlow.VisualStudio.Analytics;

namespace SpecFlow.VisualStudio.VsxStubs.ProjectSystem
{
    public class StubAnalyticsTransmitter : IAnalyticsTransmitter
    {
        private ConcurrentBag<IAnalyticsEvent> Events { get; } =new ();
        private TaskCompletionSource<IAnalyticsEvent> _taskCompletionSource = new();

        public void TransmitEvent(IAnalyticsEvent runtimeEvent)
        {
            Events.Add(runtimeEvent);
            _taskCompletionSource.TrySetResult(runtimeEvent);
        }

        public void TransmitExceptionEvent(Exception exception, Dictionary<string, object> additionalProps = null, bool? isFatal = null,
            bool anonymize = true)
        {
           //nop
        }

        public async Task<IAnalyticsEvent> WaitForEventAsync(string eventName, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var comparand = _taskCompletionSource;
                
                var analyticsEvent = Events.FirstOrDefault(ev => ev.EventName == eventName);
                if (analyticsEvent != null) return analyticsEvent;

                var original = Interlocked.CompareExchange(ref _taskCompletionSource,
                    new TaskCompletionSource<IAnalyticsEvent>(), comparand);

                await Task.WhenAny(original.Task, _taskCompletionSource.Task, Task.Delay(-1, cancellationToken));
            }

            throw new TaskCanceledException();
        }
    }
}