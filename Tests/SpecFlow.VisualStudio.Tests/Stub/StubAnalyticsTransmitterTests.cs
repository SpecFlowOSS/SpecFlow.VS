using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using FluentAssertions;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SpecFlow.VisualStudio.Analytics;
using SpecFlow.VisualStudio.VsxStubs.ProjectSystem;
using Xunit;

namespace SpecFlow.VisualStudio.Tests.Stub;

public class StubAnalyticsTransmitterTests
{
    private volatile int _i;
    private volatile int _j;

    private static ImmutableArray<T> Shuffle<T>(ImmutableArray<T> collection)
    {
        var rnd = new Random(collection.Length);
        var list = new List<T>();
        foreach (var item in collection)
        {
            var idx = rnd.Next(list.Count);
            list.Insert(idx, item);
        }

        return list.ToImmutableArray();
    }

    private static Task RunInThread(
        Action action,
        Action<Thread> initThreadAction = null)
    {
        TaskCompletionSource<bool> taskCompletionSource = new TaskCompletionSource<bool>();

        Thread thread = new Thread(() =>
        {
            try
            {
                action();
                taskCompletionSource.TrySetResult(true);
            }
            catch (Exception e)
            {
                taskCompletionSource.TrySetException(e);
            }
        });
        initThreadAction?.Invoke(thread);
        thread.Start();

        return taskCompletionSource.Task;
    }

    private static Task<TResult> RunInThread<TResult>(
        Func<Task<TResult>> action,
        Action<Thread> initThreadAction = null)
    {
        TaskCompletionSource<TResult> taskCompletionSource = new TaskCompletionSource<TResult>();

        Thread thread = new Thread(() =>
        {
            try
            {
                TResult result = action().Result;
                taskCompletionSource.TrySetResult(result);
            }
            catch (Exception e)
            {
                taskCompletionSource.TrySetException(e);
            }
        });
        initThreadAction?.Invoke(thread);
        thread.Start();

        return taskCompletionSource.Task;
    }

    [Fact]
    public async Task All_events_are_waited_without_deadlock()
    {
        //arrange
        var transmitter = new StubAnalyticsTransmitter();
        ImmutableArray<GenericEvent> events = Enumerable.Range(100, 100).Select(n => new GenericEvent($"Ev:{n}"))
            .ToImmutableArray();
        var shuffled = Shuffle(events);

        var tasks = new Task[events.Length * 2];
        for (int k = 0; k < events.Length; k++)
        {
            tasks[k * 2] = 
                RunInThread(()=> transmitter.WaitForEventAsync(events[Interlocked.Increment(ref _i) - 1].EventName));
            tasks[k * 2 + 1] =
                RunInThread(() => transmitter.TransmitEvent(shuffled[Interlocked.Increment(ref _j) - 1]));
        }

        //act
        await Task.WhenAll(tasks);

        //assert
        tasks.Select(t => (t as Task<IAnalyticsEvent>)?.Result).Where(e => e is not null).Should()
            .BeEquivalentTo(events);
    }
}
