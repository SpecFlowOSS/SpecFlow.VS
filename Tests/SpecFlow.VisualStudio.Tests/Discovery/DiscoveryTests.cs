using SpecFlow.VisualStudio.ProjectSystem.Settings;

#pragma warning disable xUnit1026 // Theory methods should use all of their parameters. Allow to use _ as identifier

namespace SpecFlow.VisualStudio.Tests.Discovery;

public class DiscoveryTests
{
    private readonly ITestOutputHelper _testOutputHelper;

    public DiscoveryTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    private Sut ArrangeSut()
    {
        var bindingRegistryCache = new StubProjectBindingRegistryCache();
        var projectScope = new InMemoryStubProjectScope(_testOutputHelper);
        var discoveryResultProvider = new StubDiscoveryResultProvider();
        projectScope.StubIdeScope.SynchronizeRunOnBackgroundThread();
        return new Sut(bindingRegistryCache, projectScope, discoveryResultProvider);
    }

    [Fact]
    public void InitializeBindingRegistryTriggersCacheUpdate()
    {
        //arrange
        var sut = ArrangeSut();
        var discoveryService = sut.BuildDiscoveryService();

        //act
        discoveryService.InitializeBindingRegistry();

        //assert
        sut.BindingRegistryCache.Verify(c =>
            c.Update(It.IsAny<Func<ProjectBindingRegistry, Task<ProjectBindingRegistry>>>()));
        sut.BindingRegistryCache.Value.Version.Should().NotBe(1);
    }

    public static Dictionary<string, Action<Sut>> EventInvokers => new()
        {
            ["WeakSettingsInitialized"] = sut=>sut.ProjectScope.StubProjectSettingsProvider.InvokeWeakSettingsInitializedEvent(),
            ["ProjectsBuilt"] = sut => sut.ProjectScope.StubIdeScope.TriggerProjectsBuilt()
        };

    public static IEnumerable<object[]> TriggersCacheUpdateOnEventsData => EventInvokers.Select(ei => new object[] {ei.Key, ei.Value});

    [Theory, MemberData(nameof(TriggersCacheUpdateOnEventsData))]
    public void TriggersCacheUpdateOnEvents(string _, Action<Sut> invokeEvent)
    {
        //arrange
        var sut = ArrangeSut();
        sut.BuildDiscoveryService();

        //act
        invokeEvent(sut);

        //assert
        sut.BindingRegistryCache.Verify(c =>
            c.Update(It.IsAny<Func<ProjectBindingRegistry, Task<ProjectBindingRegistry>>>()));
        sut.BindingRegistryCache.Value.Version.Should().NotBe(1);
    }

    [Theory, MemberData(nameof(TriggersCacheUpdateOnEventsData))]
    public void DoNotTriggersCacheUpdateOnEventsForTheSameProject(string _, Action<Sut> invokeEvent)
    {
        //arrange
        var sut = ArrangeSut();
        sut.BuildDiscoveryService();
        
        //act
        invokeEvent(sut);
        var bindingRegistry = sut.BindingRegistryCache.Value;
        invokeEvent(sut);

        //assert
        sut.BindingRegistryCache.Verify(c =>
            c.Update(It.IsAny<Func<ProjectBindingRegistry, Task<ProjectBindingRegistry>>>()), Times.Once, "the cache update have to be called only once when the project haven't changed");
        sut.BindingRegistryCache.Value.Should().BeSameAs(bindingRegistry, "the cache must not be modified");
        sut.ProjectScope.StubIdeScope.StubLogger.Messages.Where(m=>m == "Projects built or settings initialized")
            .Should().HaveCount(2, "the event is fired twice");
    }

    [Fact]
    public void DoNotDiscoverWhenProjectIsNotInitialized()
    {
        //arrange
        var sut = ArrangeSut();
        sut.ProjectScope.StubProjectSettingsProvider.Kind = DeveroomProjectKind.Uninitialized;
        DiscoveryInvoker discoveryInvoker = sut.BuildDiscoveryInvoker();

        //act
        ProjectBindingRegistry discovered = discoveryInvoker.InvokeDiscoveryWithTimer(ProjectBindingRegistry.Empty);

        //assert
        discovered.Version.Should().Be(1);
        sut.ProjectScope.StubIdeScope.StubLogger.Messages.Should().Contain("Uninitialized project settings");
    }

    [Fact]
    public void DoNotDiscoverWhenConfigSourceIsInvalid()
    {
        //arrange
        var sut = ArrangeSut();
        sut.ProjectScope.StubIdeScope.FileSystem.File.Delete(sut.ProjectScope.OutputAssemblyPath);
        DiscoveryInvoker discoveryInvoker = sut.BuildDiscoveryInvoker();

        //act
        ProjectBindingRegistry discovered = discoveryInvoker.InvokeDiscoveryWithTimer(ProjectBindingRegistry.Empty);

        //assert
        discovered.Version.Should().Be(1);
        var expected = "Test assembly not found. Please build the project to enable the SpecFlow Visual Studio Extension features.";
        sut.ProjectScope.StubIdeScope.StubLogger.Messages.Should().Contain(expected);
        sut.ProjectScope.StubIdeScope.StubErrorListServices.Errors.Should().Contain(e => e.Message == expected);
    }

    public record Sut(StubProjectBindingRegistryCache BindingRegistryCache, InMemoryStubProjectScope ProjectScope,
        StubDiscoveryResultProvider DiscoveryResultProvider)
    {
        public DiscoveryService BuildDiscoveryService() => new(ProjectScope, DiscoveryResultProvider, BindingRegistryCache);
        internal DiscoveryInvoker BuildDiscoveryInvoker() => new(ProjectScope, DiscoveryResultProvider);
    }
}
