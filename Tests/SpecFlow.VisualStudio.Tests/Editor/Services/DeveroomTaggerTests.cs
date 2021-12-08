#nullable enable
namespace SpecFlow.VisualStudio.Tests.Editor.Services;

public class DeveroomTaggerTests
{

    public DeveroomTaggerTests(ITestOutputHelper testOutputHelper)
    {
    }

    protected Sut ArrangeSut()
    {
        var actionThrottlerFactory = new Mock<IActionThrottlerFactory>(MockBehavior.Strict);
        var logger = new StubLogger();
        var ideScope = new Mock<IIdeScope>(MockBehavior.Strict);
        var projectScope = new Mock<IProjectScope>(MockBehavior.Strict);
        var propertyCollection = new PropertyCollection();
        var textBuffer = new Mock<ITextBuffer>(MockBehavior.Strict);
        var textSnapshot = new Mock<ITextSnapshot>(MockBehavior.Strict);
        var textSnapshotVersion = new Mock<ITextVersion>(MockBehavior.Strict);

        var sut = new Sut(ideScope, textBuffer, actionThrottlerFactory, textSnapshot, textSnapshotVersion);

        actionThrottlerFactory.Setup(atf => atf.Build(It.IsAny<Action>()))
            .Returns<Action>(action => sut.SetAction(action));

        ideScope.Setup(s => s.GetProject(textBuffer.Object)).Returns(projectScope.Object);
        ideScope.SetupGet(s => s.Logger).Returns(logger);
        ideScope.SetupGet(s => s.FileSystem).Returns(new MockFileSystem());
        ideScope.SetupGet(s => s.MonitoringService).Returns(
            new MonitoringService(
                new StubAnalyticsTransmitter(logger),
                Mock.Of<IWelcomeService>(),
                Mock.Of<ITelemetryConfigurationHolder>()
            ));
        ideScope.SetupGet(s => s.DeveroomErrorListServices).Returns(Mock.Of<IDeveroomErrorListServices>);
        ideScope.SetupGet(s => s.IsSolutionLoaded).Returns(true);
        ideScope.Setup(s => s.RunOnBackgroundThread(It.IsAny<Func<Task>>(), It.IsAny<Action<Exception>>()))
            .Returns((Func<Task> t, Action<Exception> e) => t());

        projectScope.SetupGet(s => s.Properties).Returns(propertyCollection);
        projectScope.SetupGet(s => s.IdeScope).Returns(ideScope.Object);
        projectScope.SetupGet(s => s.PackageReferences).Returns((NuGetPackageReference[]) null!);
        projectScope.SetupGet(s => s.OutputAssemblyPath).Returns(".");
        projectScope.SetupGet(s => s.PlatformTargetName).Returns(string.Empty);
        projectScope.SetupGet(s => s.TargetFrameworkMoniker).Returns(string.Empty);
        projectScope.SetupGet(s => s.TargetFrameworkMonikers).Returns(string.Empty);
        projectScope.SetupGet(s => s.DefaultNamespace).Returns(string.Empty);
        projectScope.SetupGet(s => s.ProjectFullName).Returns("SpecFlow.VisualStudio.Tests.dll.config");
        projectScope.SetupGet(s => s.ProjectFolder).Returns(string.Empty);
        projectScope.Setup(s => s.GetFeatureFileCount()).Returns(0);

        textBuffer.SetupGet(t => t.CurrentSnapshot).Returns(textSnapshot.Object);
        textBuffer.SetupGet(t => t.Properties).Returns(new PropertyCollection());

        textSnapshot.SetupGet(s => s.Length).Returns(0);
        textSnapshot.SetupGet(s => s.Version).Returns(textSnapshotVersion.Object);
        textSnapshot.Setup(s => s.GetText()).Returns(string.Empty);

        textSnapshotVersion.SetupGet(v => v.VersionNumber).Returns(0);

        VsxStubObjects.Initialize();

        return sut;
    }

    [Fact]
    public void GetDeveroomTagsForCaret_returns_empty_collection_after_instantiation()
    {
        //arrange
        var sut = ArrangeSut();
        DeveroomTagger tagger = sut.BuildTagger();

        //act
        IEnumerable<DeveroomTag> tags = tagger.GetDeveroomTagsForCaret(sut.BuildTextView());

        ////assert
        tags.Should().BeEmpty();
        sut.TagsChangedEvents.Should().BeEmpty();
        sut.AssertNoErrorLogged();
    }

    [Fact]
    public void GetDeveroomTagsForCaretReturns_collection_after_recalculate_action_is_triggered_by_throttler()
    {
        //arrange
        var sut = ArrangeSut();
        DeveroomTagger tagger = sut.BuildTagger();
        sut.TextSnapshot.Setup(s => s.GetText()).Returns(string.Empty);
        sut.TriggerAction();

        //act
        IEnumerable<DeveroomTag> tags = tagger.GetDeveroomTagsForCaret(sut.BuildTextView());

        ////assert
        tags.Single().Type.Should().Be("Document", "the feature file has no content");
        sut.AssertNoErrorLogged();
    }

    [Fact]
    public void TagsChanged_event_fired()
    {
        //arrange
        var sut = ArrangeSut();
        sut.BuildTagger();

        //act
        sut.TriggerAction();

        ////assert
        sut.TagsChangedEvents.Should().HaveCount(1);
        sut.AssertNoErrorLogged();
    }

    [Fact(Skip = "Doesn't work yet")] //TODO: Discuss with Gáspár
    public void TagsChanged_event_not_fired_when_triggered_with_the_same_data()
    {
        //arrange
        var sut = ArrangeSut();
        sut.BuildTagger();

        //act
        sut.TriggerAction();
        sut.TriggerAction();

        ////assert
        sut.TagsChangedEvents.Should().HaveCount(1);
        sut.AssertNoErrorLogged();
    }

    [Fact]
    public void Parsed_only_once_when_triggered_with_the_same_data()
    {
        //arrange
        var sut = ArrangeSut();
        sut.BuildTagger();

        //act
        sut.TriggerAction();
        sut.TriggerAction();

        ////assert
        sut.TextSnapshot.Verify(s => s.GetText(), Times.Once);
        sut.AssertNoErrorLogged();
    }

    protected record Sut(
        Mock<IIdeScope> IdeScope,
        Mock<ITextBuffer> TextBuffer,
        Mock<IActionThrottlerFactory> ActionThrottlerFactory,
        Mock<ITextSnapshot> TextSnapshot,
        Mock<ITextVersion> TextSnapShotVersion) : IActionThrottler
    {
        private readonly List<SnapshotSpanEventArgs> _tagsChangedEvents = new();
        private Action _recalculateAction = () => { };
        public IReadOnlyCollection<SnapshotSpanEventArgs> TagsChangedEvents => _tagsChangedEvents;

        public IEnumerable<LogMessage> LoggerMessages => (IdeScope.Object.Logger as StubLogger)!.Logs;

        public IEnumerable<LogMessage> LoggerErrorMessages =>
            LoggerMessages.Where(m => m.Level == TraceLevel.Error || m.Message.Contains("Exception"));

        public void TriggerAction(bool forceDelayed = false, bool forceDirect = false)
        {
            if (forceDelayed) return;
            _recalculateAction();
        }

        public DeveroomTagger BuildTagger()
        {
            var deveroomTagger =
                new DeveroomTagger(TextBuffer.Object, IdeScope.Object, false, ActionThrottlerFactory.Object);
            deveroomTagger.TagsChanged += DeveroomTagger_TagsChanged;
            return deveroomTagger;
        }

        private void DeveroomTagger_TagsChanged(object sender, SnapshotSpanEventArgs e)
        {
            _tagsChangedEvents.Add(e);
        }

        public StubWpfTextView BuildTextView()
        {
            return new(TextBuffer.Object);
        }

        public Sut SetAction(Action action)
        {
            _recalculateAction = action;
            return this;
        }

        public void AssertNoErrorLogged()
        {
            LoggerErrorMessages.Should().BeEmpty();
        }
    }

    [Theory]
    [InlineData(3, Skip = "Needs work")]
    [InlineData(4, Skip = "Needs work")]
    [InlineData(5, Skip = "Needs work")]
    [InlineData(6, Skip = "Needs work")]
    [InlineData(7, Skip = "Needs work")]
    public async Task Parallel(int threads)
    {
        //arrange
        var sut = ArrangeSut();
        sut.BuildTagger();

        var reCalculationInProgress = new ManualResetEvent(false);
        var cev = new CountdownEvent(threads-1);
        sut.TextSnapshot.SetupGet(s => s.Version).Returns(() =>
        {
            cev.Signal();
            reCalculationInProgress.WaitOne();
            return sut.TextSnapShotVersion.Object;
            //s.WaitOne()
        });

        var tasks = new Task[threads];
        for (int i = 0; i < threads; i++)
            tasks[i] = Task.Run(() => sut.TriggerAction());

        //act
        cev.Wait(TimeSpan.FromMilliseconds(100));
        cev.CurrentCount.Should().Be(0);
        cev.Reset((threads-1)*2);
        reCalculationInProgress.Set();
        await Task.WhenAll(tasks);

        ////assert
        sut.TextSnapshot.Verify(s => s.GetText(), Times.Exactly(2));
        sut.AssertNoErrorLogged();
    }
}