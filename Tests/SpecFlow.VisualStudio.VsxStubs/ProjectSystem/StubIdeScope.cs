namespace SpecFlow.VisualStudio.VsxStubs.ProjectSystem;

public class StubIdeScope : Mock<IIdeScope>, IIdeScope, IDisposable
{
    public StubIdeScope(ITestOutputHelper testOutputHelper) : base(MockBehavior.Strict)
    {
        AnalyticsTransmitter = new StubAnalyticsTransmitter(Logger);
        MonitoringService =
            new MonitoringService(
                AnalyticsTransmitter,
                new Mock<IWelcomeService>().Object,
                new Mock<ITelemetryConfigurationHolder>().Object);

        CompositeLogger.Add(new DeveroomXUnitLogger(testOutputHelper));
        CompositeLogger.Add(StubLogger);
        Actions = new StubIdeActions(this);
        VsxStubObjects.Initialize();

        SetupFireAndForget();
        SetupFireAndForgetOnBackgroundThread();

        CurrentTextView = StubWpfTextView.CreateTextView(this, new TestText(Array.Empty<string>()));
        BackgroundTaskTokenSource = new DebuggableCancellationTokenSource(TimeSpan.FromSeconds(20));
    }

    public CancellationTokenSource BackgroundTaskTokenSource { get; }
    public StubAnalyticsTransmitter AnalyticsTransmitter { get; }
    public IDictionary<string, StubWpfTextView> OpenViews { get; } = new Dictionary<string, StubWpfTextView>();
    public StubLogger StubLogger { get; } = new();

    public DeveroomCompositeLogger CompositeLogger { get; } = new()
    {
        new DeveroomDebugLogger()
    };

    public StubWindowManager StubWindowManager { get; } = new();
    public List<IProjectScope> ProjectScopes { get; } = new();
    public IWpfTextView CurrentTextView { get; internal set; }
    public StubErrorListServices StubErrorListServices { get; } = new();

    public bool IsSolutionLoaded { get; } = true;

    public IProjectScope GetProject(ITextBuffer textBuffer) =>
        textBuffer.Properties.GetProperty<IProjectScope>(typeof(IProjectScope));

    public IDeveroomLogger Logger => CompositeLogger;
    public IIdeActions Actions { get; set; }
    public IDeveroomWindowManager WindowManager => StubWindowManager;
    public IFileSystem FileSystem { get; private set; } = new MockFileSystem();

    public IDeveroomOutputPaneServices DeveroomOutputPaneServices { get; } =
        new Mock<IDeveroomOutputPaneServices>().Object;

    public IDeveroomErrorListServices DeveroomErrorListServices => StubErrorListServices;
    public IMonitoringService MonitoringService { get; }

    [CanBeNull] public event EventHandler<EventArgs> WeakProjectsBuilt = null!;
    [CanBeNull] public event EventHandler<EventArgs> WeakProjectOutputsUpdated = null!;

    public void CalculateSourceLocationTrackingPositions(IEnumerable<SourceLocation> sourceLocations)
    {
    }

    public bool GetTextBuffer(SourceLocation sourceLocation, out ITextBuffer textBuffer)
    {
        if (OpenViews.TryGetValue(sourceLocation.SourceFile, out var view))
        {
            textBuffer = view.TextBuffer;
            return true;
        }

        textBuffer = default;
        return false;
    }

    public SyntaxTree GetSyntaxTree(ITextBuffer textBuffer)
    {
        var fileContent = textBuffer.CurrentSnapshot.GetText();
        return CSharpSyntaxTree.ParseText(fileContent);
    }

    public void FireAndForget(Func<Task> action, Action<Exception> onException,
        [CallerMemberName] string callerName = "???")
        => Object.FireAndForget(action, onException, callerName);

    private void SetupFireAndForget()
    {
        Setup(s => s.FireAndForget(It.IsAny<Func<Task>>(), It.IsAny<Action<Exception>>(),
                It.IsAny<string>()))
            .Callback((Func<Task> action, Action<Exception> onException, string _) =>
            {
                try
                {
#pragma warning disable VSTHRD002
                    action().Wait();
#pragma warning restore
                }
                catch (Exception e)
                {
                    Logger.LogException(MonitoringService, e);
                    onException(e);
                }
            });
    }

    public void FireAndForgetOnBackgroundThread(Func<CancellationToken, Task> action, string callerName = "???") =>
        Object.FireAndForgetOnBackgroundThread(action, callerName);

    private volatile int _taskId = 0;

    private ImmutableDictionary<string, Func<CancellationToken, Task>> BackGroundTasks { get; set; } =
        ImmutableDictionary<string, Func<CancellationToken, Task>>.Empty;

    public Task StartAndWaitAllBackgroundTasks()
    {
        var allTasks =
            BackGroundTasks.Values.Select(t => Task.Run(async () => await t(BackgroundTaskTokenSource.Token)));
        BackGroundTasks = BackGroundTasks.Clear();
        return Task.WhenAll(allTasks);
    }

    private void SetupFireAndForgetOnBackgroundThread()
    {
        Setup(s => s.FireAndForgetOnBackgroundThread(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<string>()))
            .Callback((Func<CancellationToken, Task> action, string callerName) =>
                BackGroundTasks = BackGroundTasks.Add($"{Interlocked.Increment(ref _taskId)}:{callerName}", action));
    }

    public Task RunOnUiThread(Action action)
    {
        action();
        return Task.CompletedTask;
    }

    public void OpenIfNotOpened(string path)
    {
        if (OpenViews.TryGetValue(path, out _))
            return;

        var lines = FileSystem.File.ReadAllLines(path);
        CreateTextView(new TestText(lines), filePath: path);
    }

    public IProjectScope[] GetProjectsWithFeatureFiles() => ProjectScopes.ToArray();

    public IDisposable CreateUndoContext(string undoLabel) => null;

    public StubWpfTextView CreateTextView(TestText inputText, string newLine = null, IProjectScope projectScope = null,
        string contentType = VsContentTypes.FeatureFile, string filePath = null)
    {
        if (filePath != null && !Path.IsPathRooted(filePath) && projectScope != null)
            filePath = Path.Combine(projectScope.ProjectFolder, filePath);

        if (projectScope == null && filePath != null)
            projectScope = ProjectScopes.FirstOrDefault(p =>
                (p as InMemoryStubProjectScope)?.FilesAdded.Any(f => f.Key == filePath) ?? false);

        var textView = StubWpfTextView.CreateTextView(this, inputText, newLine, projectScope, contentType, filePath);
        if (filePath != null)
        OpenViews[filePath] = textView;

        CurrentTextView = textView;

        return textView;
    }

    public IWpfTextView EnsureOpenTextView(SourceLocation sourceLocation)
    {
        if (OpenViews.TryGetValue(sourceLocation.SourceFile, out var view))
            return view;

        var lines = FileSystem.File.ReadAllLines(sourceLocation.SourceFile);
        var textView = CreateTextView(new TestText(lines), filePath: sourceLocation.SourceFile);
        return textView;
    }

    public void TriggerProjectsBuilt()
    {
        WeakProjectsBuilt?.Invoke(this, EventArgs.Empty);
        WeakProjectOutputsUpdated?.Invoke(this, EventArgs.Empty);
    }

    public void UsePhysicalFileSystem()
    {
        FileSystem = new FileSystem();
    }

    public void Dispose()
    {
        BackgroundTaskTokenSource.Cancel();
        BackgroundTaskTokenSource.Dispose();
    }
}
