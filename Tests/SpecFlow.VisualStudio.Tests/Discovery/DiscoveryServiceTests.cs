using System.Collections.Concurrent;
using SpecFlow.VisualStudio.Annotations;
using SpecFlow.VisualStudio.ProjectSystem.Settings;

namespace SpecFlow.VisualStudio.Tests.Discovery;

public class DiscoveryServiceTests
{
    private ITestOutputHelper _testOutputHelper;

    public DiscoveryServiceTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }


    [Fact]
    public async Task ParallelUpdate()
    {
        //arrange
        var start = DateTimeOffset.UtcNow;
        var projectScope = new Mock<IProjectScope>(MockBehavior.Strict);
        var ideScope = new Mock<IIdeScope>(MockBehavior.Strict);
        var stubLogger = new StubLogger();
        var logger = new DeveroomCompositeLogger();
        logger.Add(new DeveroomXUnitLogger(_testOutputHelper));
        logger.Add(stubLogger);

        var propertyCollection = new PropertyCollection();
        var projectSettingsProvider = new Mock<IProjectSettingsProvider>(MockBehavior.Strict);
        var discoveryResultProvider = new Mock<IDiscoveryResultProvider>(MockBehavior.Strict);
        var projectSettings = new ProjectSettings(
            DeveroomProjectKind.SpecFlowTestProject,
            default, 
            TargetFrameworkMoniker.CreateFromShortName(string.Empty), 
            default, 
            default, 
            default, 
            default, 
            default, 
            string.Empty, 
            default, 
            string.Empty
        );

        projectSettingsProvider.Setup(p => p.GetProjectSettings()).Returns(projectSettings);

        propertyCollection.AddProperty(typeof(IProjectSettingsProvider), projectSettingsProvider.Object);

        ideScope.SetupGet(s => s.Logger).Returns(logger);
        ideScope.SetupGet(s => s.FileSystem).Returns(new MockFileSystem());
        ideScope.SetupGet(s => s.MonitoringService).Returns(
            new MonitoringService(
                new StubAnalyticsTransmitter(logger),
                Mock.Of<IWelcomeService>(),
                Mock.Of<ITelemetryConfigurationHolder>()
            ));
        ideScope.SetupGet(s => s.DeveroomErrorListServices).Returns(Mock.Of<IDeveroomErrorListServices>);
        ideScope.Setup(s => s.CalculateSourceLocationTrackingPositions(It.IsAny<IEnumerable<SourceLocation>>()));

        projectScope.SetupGet(p => p.IdeScope).Returns(ideScope.Object);
        projectScope.SetupGet(p => p.Properties).Returns(propertyCollection);
        projectScope.SetupGet(p => p.ProjectName).Returns(string.Empty);

        discoveryResultProvider
            .Setup(p => p.RunDiscovery(string.Empty, string.Empty, projectSettings))
            .Returns(new DiscoveryResult{StepDefinitions = Array.Empty<StepDefinition>()});

        var discoveryService = new DiscoveryService(projectScope.Object, discoveryResultProvider.Object, new ProjectBindingRegistryCache(ideScope.Object));

        var oldVersions = new ConcurrentQueue<int>();
        var initialRegistry = new ProjectBindingRegistry(Array.Empty<ProjectStepDefinitionBinding>(), 123456);

        //act
        var updateTaskCount = 0;
        bool retried;
        var timeout = TimeSpan.FromSeconds(10);
        var cts = new CancellationTokenSource(timeout);
        try
        {
            do
            {
                var tasks = new Task[10];
                for (int i = 0; i < tasks.Length; ++i)
                {
                    tasks[i] = RunInThread(async () =>
                    {
                        for (int j = 0; j < 5 + DateTimeOffset.UtcNow.Ticks % 10; ++j)
                            if ((i + j) % 7 == 6) await discoveryService.BindingRegistryCache.GetLatest();

                            else
                            {
                                Interlocked.Increment(ref updateTaskCount);
                                await discoveryService.BindingRegistryCache.Update(old =>
                                {
                                    oldVersions.Enqueue(old.Version);
                                return new ProjectBindingRegistry(Array.Empty<ProjectStepDefinitionBinding>(), i*1000+j);
                                });
                            }
                    }, cts.Token);
                }

                await Task.WhenAll(tasks);
                retried = stubLogger.Logs.Any(log => log.Message.Contains("in 2 iteration"));
                stubLogger.Clear();
            } while (!retried && !cts.IsCancellationRequested);
        }
        catch (OperationCanceledException e)
        {
            return;
        }
        if (cts.IsCancellationRequested) return;

        //assert
        var finish = DateTimeOffset.UtcNow;
        //cts.IsCancellationRequested.Should().BeFalse($"started at {start} and not finished until {finish}");
        //(finish - start).Should().BeLessThan(timeout, $"started at {start} and not finished until {finish}");
        var registry = await discoveryService.BindingRegistryCache.GetLatest();
        registry.Version.Should().BeGreaterOrEqualTo(initialRegistry.Version + updateTaskCount);
        oldVersions.Count.Should().Be(updateTaskCount);
        oldVersions.Should().BeInAscendingOrder();
    }

    private Task RunInThread(Func<Task> action, CancellationToken ct)
    {
        TaskCompletionSource<bool> taskCompletionSource = new TaskCompletionSource<bool>();

        ThreadPool.QueueUserWorkItem(_ =>
        {
            try
            {
                action().Wait(ct);
                taskCompletionSource.TrySetResult(true);
            }
            catch (Exception e)
            {
                taskCompletionSource.TrySetException(e);
            }
        }, default);

        return taskCompletionSource.Task;
    }
}
