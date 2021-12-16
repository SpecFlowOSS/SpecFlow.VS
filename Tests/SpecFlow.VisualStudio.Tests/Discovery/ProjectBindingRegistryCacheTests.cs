namespace SpecFlow.VisualStudio.Tests.Discovery;

public class ProjectBindingRegistryCacheTests
{
    private readonly ITestOutputHelper _testOutputHelper;

    public ProjectBindingRegistryCacheTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Fact]
    public async Task CannotRevertToAnOlderVersionOfBindingRegistry()
    {
        //arrange
        var ideScope = new StubIdeScope(_testOutputHelper);
        var cache = new ProjectBindingRegistryCache(ideScope);
        var olderRegistry = ProjectBindingRegistry.FromStepDefinitions(Array.Empty<ProjectStepDefinitionBinding>());
        var newerRegistry = ProjectBindingRegistry.FromStepDefinitions(Array.Empty<ProjectStepDefinitionBinding>());

        //act
        await cache.Update(_ => newerRegistry);

        Func<Task> secondUpdateWithOlderRegistry = () => cache.Update(_ => olderRegistry);

        //assert
        await secondUpdateWithOlderRegistry.Should().ThrowAsync<InvalidOperationException>();
        cache.Value.Should().BeSameAs(newerRegistry);
    }

    [Fact]
    public async Task DoNotRevertToAnInvalidBindingRegistry()
    {
        //arrange
        var ideScope = new StubIdeScope(_testOutputHelper);
        var cache = new ProjectBindingRegistryCache(ideScope);
        var existingRegistry = ProjectBindingRegistry.FromStepDefinitions(Array.Empty<ProjectStepDefinitionBinding>());
        var invalidRegistry = ProjectBindingRegistry.Invalid;

        //act
        await cache.Update(_ => existingRegistry);
        await cache.Update(_ => invalidRegistry);

        //assert
        cache.Value.Should().BeSameAs(existingRegistry);
        ideScope.StubLogger.Messages.Should().Contain("Got an invalid registry, ignoring...");
    }

    [Fact]
    public async Task UpdatesBindingRegistry()
    {
        //arrange
        var ideScope = new StubIdeScope(_testOutputHelper);
        var cache = new ProjectBindingRegistryCache(ideScope);
        var bindingRegistry = ProjectBindingRegistry.FromStepDefinitions(new ProjectStepDefinitionBinding[]
        {
            new TestProjectStepDefinitionBinding(),
            new TestProjectStepDefinitionBinding("Error")
        });

        //act
        await cache.Update(_ => bindingRegistry);

        //assert
        cache.Value.Should().BeSameAs(bindingRegistry);
    }

    [Fact(Skip = "Sporadically fails on Azure")]
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
        var timeout = TimeSpan.FromSeconds(20);
        var cts = new CancellationTokenSource(timeout);
        int i = 0;
        try
        {
            RunInThread(async () =>
            {
                var priorVer = 0;
                while (!cts.IsCancellationRequested)
                {
                    var bindingRegistry = await projectBindingRegistryCache.GetLatest();
                    priorVer.Should().BeLessOrEqualTo(bindingRegistry.Version);
                    priorVer = bindingRegistry.Version;
                    await Task.Delay(10, cts.Token);
                }
            }, cts.Token);

            for (i = 0; i < 1000 && !cts.IsCancellationRequested; ++i)
            {
                RunInThread(async () =>
                {
                    while (!cts.IsCancellationRequested)
                    {
                        Interlocked.Increment(ref updateTaskCount);
                        await projectBindingRegistryCache.Update(async old =>
                        {
                            await Task.Yield();
                            oldVersions.Enqueue(old.Version);
                            return new ProjectBindingRegistry(Array.Empty<ProjectStepDefinitionBinding>(),
                                Guid.NewGuid().GetHashCode());
                        });


                        await Task.Yield();

                        if (stubLogger.Logs.Any(log => log.Message.Contains("in 2 iteration")))
                            cts.Cancel();
                    }
                }, cts.Token);
                await Task.Delay(i / 10, cts.Token);
            }
        }
        catch (OperationCanceledException e)
        {
            _testOutputHelper.WriteLine(e.ToString());
        }

        //assert
        var finish = DateTimeOffset.UtcNow;
        _testOutputHelper.WriteLine($"i:{i} cancelled:{cts.IsCancellationRequested}");
        (finish - start).Should().BeLessThan(timeout, $"started at {start} and not finished until {finish}");
        var registry = await projectBindingRegistryCache.GetLatest();
        registry.Should().BeSameAs(projectBindingRegistryCache.Value);
        registry.Version.Should().BeGreaterOrEqualTo(initialRegistry.Version + updateTaskCount);
        oldVersions.Count.Should().Be(updateTaskCount);
        oldVersions.Should().BeInAscendingOrder();
    }

    private void RunInThread(Func<Task> action, CancellationToken ct)
    {
        var thread = new Thread(_ =>
        {
            try
            {
#pragma warning disable VSTHRD002
                action().Wait(ct);
#pragma warning restore
            }
            catch (Exception e)
            {
                _testOutputHelper.WriteLine(e.ToString());
            }
        }) {IsBackground = true, Priority = ThreadPriority.Highest};
        thread.Start();
    }

    private class TestProjectStepDefinitionBinding : ProjectStepDefinitionBinding
    {
        public TestProjectStepDefinitionBinding()
            : base(ScenarioBlock.Given, new Regex(string.Empty), new Scope(),
                new ProjectStepDefinitionImplementation("M1", Array.Empty<string>(), new SourceLocation("file", 0, 0)))
        {
        }

        public TestProjectStepDefinitionBinding(string error)
            : base(ScenarioBlock.Given, new Regex(string.Empty), new Scope(),
                new ProjectStepDefinitionImplementation("M1", Array.Empty<string>(), new SourceLocation("file", 0, 0)),
                "", error)
        {
        }
    }
}
