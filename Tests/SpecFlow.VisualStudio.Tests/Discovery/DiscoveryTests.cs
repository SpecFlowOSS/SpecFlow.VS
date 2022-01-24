﻿#pragma warning disable xUnit1026 // Theory methods should use all of their parameters. Allow to use _ as identifier

namespace SpecFlow.VisualStudio.Tests.Discovery;

public class DiscoveryTests
{
    private readonly ITestOutputHelper _testOutputHelper;

    public DiscoveryTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    public static Dictionary<string, Action<Sut>> EventInvokers => new()
    {
        ["WeakSettingsInitialized"] = sut =>
            sut.ProjectScope.StubProjectSettingsProvider.InvokeWeakSettingsInitializedEvent(),
        ["ProjectsBuilt"] = sut => sut.ProjectScope.StubIdeScope.TriggerProjectsBuilt()
    };

    public static IEnumerable<object[]> TriggersCacheUpdateOnEventsData =>
        EventInvokers.Select(ei => new object[] {ei.Key, ei.Value});

    private Sut ArrangeSut()
    {
        var bindingRegistryCache = new StubProjectBindingRegistryCache();
        var projectScope = new InMemoryStubProjectScope(_testOutputHelper);
        var discoveryResultProvider = new StubDiscoveryResultProvider();
#pragma warning disable VSTHRD002
        projectScope.StubIdeScope
            .Setup(s => s.FireAndForgetOnBackgroundThread(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<string>()))
            .Callback((Func<CancellationToken, Task> action, string _) 
                => action(projectScope.StubIdeScope.BackgroundTaskTokenSource.Token).Wait());
#pragma warning restore VSTHRD002

        InMemoryStubProjectBuilder.CreateOutputAssembly(projectScope);

        return new Sut(bindingRegistryCache, projectScope, discoveryResultProvider);
    }

    [Fact]
    public void TriggerDiscoveryUpdatesTheCache()
    {
        //arrange
        using var sut = ArrangeSut();
        var discoveryService = sut.BuildDiscoveryService();

        //act
        discoveryService.TriggerDiscovery();

        //assert
        sut.BindingRegistryCache.Verify(c =>
            c.Update(It.IsAny<Func<ProjectBindingRegistry, Task<ProjectBindingRegistry>>>()));
        sut.BindingRegistryCache.Value.Version.Should().NotBe(1);
    }

    [Theory]
    [MemberData(nameof(TriggersCacheUpdateOnEventsData))]
    public void TriggersCacheUpdateOnEvents(string _, Action<Sut> triggerEvent)
    {
        //arrange
        using var sut = ArrangeSut();
        sut.BuildDiscoveryService();

        //act
        triggerEvent(sut);

        //assert
        sut.BindingRegistryCache.Verify(c =>
            c.Update(It.IsAny<Func<ProjectBindingRegistry, Task<ProjectBindingRegistry>>>()));
        sut.BindingRegistryCache.Value.Version.Should().NotBe(1);
    }

    [Theory]
    [MemberData(nameof(TriggersCacheUpdateOnEventsData))]
    public void DoNotTriggersCacheUpdateOnEventsForTheSameProject(string _, Action<Sut> triggerEvent)
    {
        //arrange
        using var sut = ArrangeSut();
        sut.BuildDiscoveryService();

        //act
        triggerEvent(sut);
        var bindingRegistry = sut.BindingRegistryCache.Value;
        triggerEvent(sut);

        //assert
        sut.BindingRegistryCache.Verify(c =>
                c.Update(It.IsAny<Func<ProjectBindingRegistry, Task<ProjectBindingRegistry>>>()), Times.Once,
            "the cache update have to be called only once when the project haven't changed");
        sut.BindingRegistryCache.Value.Should().BeSameAs(bindingRegistry, "the cache must not be modified");
        sut.ProjectScope.StubIdeScope.StubLogger.Messages.Where(m => m == "Projects built or settings initialized")
            .Should().HaveCount(2, "the event is fired twice");
    }

    [Fact]
    public void DoNotDiscoverWhenProjectIsNotInitialized()
    {
        //arrange
        using var sut = ArrangeSut();
        sut.ProjectScope.StubProjectSettingsProvider.Kind = DeveroomProjectKind.Uninitialized;

        DiscoveryInvoker discoveryInvoker = sut.BuildDiscoveryInvoker();

        //act
        ProjectBindingRegistry discovered = discoveryInvoker.InvokeDiscoveryWithTimer();

        //assert
        discovered.Version.Should().Be(1);
        sut.ProjectScope.StubIdeScope.StubLogger.Messages.Should().Contain("Uninitialized project settings");
    }

    [Fact]
    public void DoNotDiscoverWhenProjectIsNotSpecFlowTestProject()
    {
        //arrange
        using var sut = ArrangeSut();
        sut.ProjectScope.StubProjectSettingsProvider.Kind = DeveroomProjectKind.SpecFlowLibProject;

        DiscoveryInvoker discoveryInvoker = sut.BuildDiscoveryInvoker();

        //act
        ProjectBindingRegistry discovered = discoveryInvoker.InvokeDiscoveryWithTimer();

        //assert
        discovered.Version.Should().Be(1);
        sut.ProjectScope.StubIdeScope.StubLogger.Messages.Should().Contain("Non-SpecFlow test project");
    }

    [Fact]
    public void DoNotDiscoverWhenConfigSourceIsInvalid()
    {
        //arrange
        using var sut = ArrangeSut();
        sut.ProjectScope.StubIdeScope.FileSystem.File.Delete(sut.ProjectScope.OutputAssemblyPath);
        DiscoveryInvoker discoveryInvoker = sut.BuildDiscoveryInvoker();

        //act
        ProjectBindingRegistry discovered = discoveryInvoker.InvokeDiscoveryWithTimer();

        //assert
        discovered.Version.Should().Be(1);
        var expected =
            "Test assembly not found. Please build the project to enable the SpecFlow Visual Studio Extension features.";
        sut.ProjectScope.StubIdeScope.StubLogger.Messages.Should().Contain(expected);
        sut.ProjectScope.StubIdeScope.StubErrorListServices.Errors.Should().Contain(e => e.Message == expected);
    }

    [Fact]
    public void DoNotDiscoverWhenDiscoveryFails()
    {
        //arrange
        using var sut = ArrangeSut();
        sut.DiscoveryResultProvider.DiscoveryResult = new DiscoveryResult
        {
            StepDefinitions = new StepDefinition[]
            {
                new TestStepDefinition
                {
                    Error = nameof(DoNotDiscoverWhenDiscoveryFails),
                    TestSourceLocation = new SourceLocation(string.Empty, 0, 0),
                    Method = "Foo"
                }
            }
        };
        DiscoveryInvoker discoveryInvoker = sut.BuildDiscoveryInvoker();

        //act
        ProjectBindingRegistry discovered = discoveryInvoker.InvokeDiscoveryWithTimer();

        //assert
        discovered.Version.Should().NotBe(1);
        sut.ProjectScope.StubIdeScope.StubLogger.Messages.Should()
            .Contain(m => m.Contains("Invalid step definitions found"));
        sut.ProjectScope.StubIdeScope.StubErrorListServices.Errors.Should()
            .Contain(e => e.Message.Contains(nameof(DoNotDiscoverWhenDiscoveryFails)));
    }

    [Fact]
    public void InvalidStepDefinitionsAreReported()
    {
        //arrange
        using var sut = ArrangeSut();
        sut.DiscoveryResultProvider.DiscoveryResult = new DiscoveryResult
        {
            ErrorMessage = nameof(DoNotDiscoverWhenDiscoveryFails)
        };
        DiscoveryInvoker discoveryInvoker = sut.BuildDiscoveryInvoker();

        //act
        ProjectBindingRegistry discovered = discoveryInvoker.InvokeDiscoveryWithTimer();

        //assert
        discovered.Version.Should().Be(1);
        sut.ProjectScope.StubIdeScope.StubLogger.Messages.Should()
            .Contain(sut.DiscoveryResultProvider.DiscoveryResult.ErrorMessage);
        var expected = "The project bindings (e.g. step definitions) could not be discovered.";
        sut.ProjectScope.StubIdeScope.StubLogger.Messages.Should().Contain(m => m.Contains(expected));
        sut.ProjectScope.StubIdeScope.StubErrorListServices.Errors.Should().Contain(e => e.Message.Contains(expected));
    }

    public record Sut(StubProjectBindingRegistryCache BindingRegistryCache, InMemoryStubProjectScope ProjectScope,
        StubDiscoveryResultProvider DiscoveryResultProvider) : IDisposable
    {
        public DiscoveryService BuildDiscoveryService() =>
            new(ProjectScope, DiscoveryResultProvider, BindingRegistryCache);

        internal DiscoveryInvoker BuildDiscoveryInvoker() => new(ProjectScope, DiscoveryResultProvider);

        public void Dispose()
        {
            ProjectScope.StubIdeScope.Dispose();
        }
    }
}
