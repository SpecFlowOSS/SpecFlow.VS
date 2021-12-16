#nullable enable

namespace SpecFlow.VisualStudio.ProjectSystem;

public interface IIdeScope
{
    bool IsSolutionLoaded { get; }
    IDeveroomLogger Logger { get; }
    IMonitoringService MonitoringService { get; }
    IIdeActions Actions { get; }
    IDeveroomWindowManager WindowManager { get; }
    IDeveroomOutputPaneServices DeveroomOutputPaneServices { get; }
    IDeveroomErrorListServices DeveroomErrorListServices { get; }
    IFileSystem FileSystem { get; }
    IProjectScope GetProject(ITextBuffer textBuffer);
    event EventHandler<EventArgs> WeakProjectsBuilt;
    event EventHandler<EventArgs> WeakProjectOutputsUpdated;

    void CalculateSourceLocationTrackingPositions(IEnumerable<SourceLocation> sourceLocations);
    IProjectScope[] GetProjectsWithFeatureFiles();

    IDisposable CreateUndoContext(string undoLabel);
    bool GetTextBuffer(SourceLocation sourceLocation, out ITextBuffer textBuffer);
    SyntaxTree GetSyntaxTree(ITextBuffer textBuffer);

    Task RunOnBackgroundThread(Func<Task> action, Action<Exception> onException,
        [CallerMemberName] string callerName = "???");

    Task RunOnUiThread(Action action);
    void OpenIfNotOpened(string path);
}
