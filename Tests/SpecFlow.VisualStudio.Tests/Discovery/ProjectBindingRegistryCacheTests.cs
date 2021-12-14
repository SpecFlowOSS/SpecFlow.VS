namespace SpecFlow.VisualStudio.Tests.Discovery;

public class ProjectBindingRegistryCacheTests
{
    private readonly ITestOutputHelper _testOutputHelper;

    public ProjectBindingRegistryCacheTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Fact]
    public async Task ParallelUpdate()
    {
        //arrange
        var start = DateTimeOffset.UtcNow;
        var ideScope = new Mock<IIdeScope>(MockBehavior.Strict);
        var stubLogger = new StubLogger();
        var logger = new DeveroomCompositeLogger();
        logger.Add(new DeveroomXUnitLogger(_testOutputHelper));
        logger.Add(stubLogger);

        ideScope.SetupGet(s => s.Logger).Returns(logger);
        ideScope.Setup(s => s.CalculateSourceLocationTrackingPositions(It.IsAny<IEnumerable<SourceLocation>>()));

        var projectBindingRegistryCache = new ProjectBindingRegistryCache(ideScope.Object);

        var oldVersions = new ConcurrentQueue<int>();
        var initialRegistry = new ProjectBindingRegistry(Array.Empty<ProjectStepDefinitionBinding>(), 123456);

        //act
        var updateTaskCount = 0;
        var timeout = TimeSpan.FromSeconds(10);
        var cts = new CancellationTokenSource(timeout);
        int fireGetLatestCounter = 0;
        try
        {
            bool retried = false;
            do
            {
                var tasks = new List<Task>();
                for (int i = 0; i < 10 && !retried; ++i)
                    tasks.Add(RunInThread(async () =>
                    {
                        for (int j = 0; j < 5 + DateTimeOffset.UtcNow.Ticks % 10 && !retried; ++j)
                        {
                            if (Interlocked.Increment(ref fireGetLatestCounter) % 7 == 6)
                            {
                                await projectBindingRegistryCache.GetLatest();
                            }
                            else
                            {
                                Interlocked.Increment(ref updateTaskCount);
                                await projectBindingRegistryCache.Update(async old =>
                                {
                                    await Task.Yield();
                                    oldVersions.Enqueue(old.Version);
                                    return new ProjectBindingRegistry(Array.Empty<ProjectStepDefinitionBinding>(),
                                        Guid.NewGuid().GetHashCode());
                                });
                            }

                            retried |= stubLogger.Logs.Any(log => log.Message.Contains("in 2 iteration"));
                        }
                    }, cts.Token));

                await Task.WhenAll(tasks);
                stubLogger.Clear();
            } while (!retried && !cts.IsCancellationRequested);
        }
        catch (OperationCanceledException e)
        {
            _testOutputHelper.WriteLine(e.ToString());
        }

        //assert
        var finish = DateTimeOffset.UtcNow;
        cts.IsCancellationRequested.Should().BeFalse($"started at {start} and not finished until {finish}");
        (finish - start).Should().BeLessThan(timeout, $"started at {start} and not finished until {finish}");
        var registry = await projectBindingRegistryCache.GetLatest();
        registry.Should().BeSameAs(projectBindingRegistryCache.Value);
        registry.Version.Should().BeGreaterOrEqualTo(initialRegistry.Version + updateTaskCount);
        oldVersions.Count.Should().Be(updateTaskCount);
        oldVersions.Should().BeInAscendingOrder();
    }

    private Task RunInThread(Func<Task> action, CancellationToken ct)
    {
        TaskCompletionSource<bool> taskCompletionSource = new TaskCompletionSource<bool>();

        var thread = new Thread(_ =>
        {
            try
            {
                action().Wait(ct);
                taskCompletionSource.TrySetResult(true);
            }
            catch (Exception e)
            {
                _testOutputHelper.WriteLine(e.ToString());
                taskCompletionSource.TrySetException(e);
            }
        }, default);
        thread.Start();

        return taskCompletionSource.Task;
    }
}
