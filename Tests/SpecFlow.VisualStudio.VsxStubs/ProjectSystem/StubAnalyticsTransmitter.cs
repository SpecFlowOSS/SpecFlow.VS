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
        TaskCompletionSource<IAnalyticsEvent> tcs = new TaskCompletionSource<IAnalyticsEvent>();

        public void TransmitEvent(IAnalyticsEvent runtimeEvent)
        {
            Events.Add(runtimeEvent);
            tcs.TrySetResult(runtimeEvent);
        }

        public void TransmitExceptionEvent(Exception exception, Dictionary<string, object> additionalProps = null, bool? isFatal = null,
            bool anonymize = true)
        {
           //throw new NotImplementedException();
        }

        public async Task<IAnalyticsEvent> WaitForEvent(string eventName, CancellationToken cancellationToken)
        {
            Task t = null;
            while (!cancellationToken.IsCancellationRequested)
            {
                var comparand = tcs;
                
                var analyticsEvent = Events.FirstOrDefault(ev => ev.EventName == eventName);
                if (analyticsEvent != null) return analyticsEvent;

                var original = Interlocked.CompareExchange(ref tcs,
                    new TaskCompletionSource<IAnalyticsEvent>(), comparand);

                t = await Task.WhenAny(original.Task, tcs.Task, Task.Delay(-1, cancellationToken));
            }

            throw new TaskCanceledException();
        }
    }
}