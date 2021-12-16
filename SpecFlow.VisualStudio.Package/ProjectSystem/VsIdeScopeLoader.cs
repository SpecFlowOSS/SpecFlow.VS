using Microsoft.CodeAnalysis;
using Project = EnvDTE.Project;

namespace SpecFlow.VisualStudio.ProjectSystem;

[Export(typeof(IIdeScope))]
public class VsIdeScopeLoader : IVsIdeScope
{
    private readonly Lazy<IVsIdeScope> _projectSystemReference;
    private readonly IDeveroomLogger _safeLogger;
    private readonly IMonitoringService _safeMonitoringService;
    private readonly IServiceProvider _serviceProvider;

    [ImportingConstructor]
    public VsIdeScopeLoader([Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        // ReSharper disable once RedundantArgumentDefaultValue
        _safeLogger = GetSafeLogger();
        _safeMonitoringService = GetSafeMonitoringService(serviceProvider);
        _projectSystemReference =
            new Lazy<IVsIdeScope>(LoadProjectSystem, LazyThreadSafetyMode.ExecutionAndPublication);
    }

    private IVsIdeScope VsIdeScope => _projectSystemReference.Value;
    private bool IsLoaded => _projectSystemReference.IsValueCreated;

    private IMonitoringService GetSafeMonitoringService(IServiceProvider serviceProvider)
    {
        try
        {
            var safeMonitoringService = VsUtils.ResolveMefDependency<IMonitoringService>(serviceProvider);
            if (safeMonitoringService != null)
                _safeLogger.LogVerbose("Monitoring service loaded");
            return safeMonitoringService ?? NullVsIdeScope.GetNullMonitoringService();
        }
        catch
        {
            return NullVsIdeScope.GetNullMonitoringService();
        }
    }

    private static IDeveroomLogger GetSafeLogger()
    {
        try
        {
            return new DeveroomFileLogger();
        }
        catch
        {
            return new DeveroomNullLogger();
        }
    }

    private IVsIdeScope LoadProjectSystem()
    {
        _safeLogger.LogVerbose("Loading VsIdeScope...");
        try
        {
            MonitorLoadProjectSystem();
            return VsUtils.ResolveMefDependency<VsIdeScope>(_serviceProvider);
        }
        catch (Exception ex)
        {
            var nullVsProjectSystem = new NullVsIdeScope(_safeLogger, _serviceProvider, _safeMonitoringService);
            ReportErrorServices.ReportInitError(nullVsProjectSystem, ex);
            return nullVsProjectSystem;
        }
    }

    private void MonitorLoadProjectSystem()
    {
        try
        {
            _safeMonitoringService.MonitorLoadProjectSystem();
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }
    }

    #region Delegating members

    public bool IsSolutionLoaded => VsIdeScope.IsSolutionLoaded;

    public IProjectScope GetProject(ITextBuffer textBuffer) => VsIdeScope.GetProject(textBuffer);

    public IDeveroomLogger Logger => VsIdeScope.Logger;
    public IMonitoringService MonitoringService => VsIdeScope.MonitoringService;

    public IIdeActions Actions => VsIdeScope.Actions;

    public IDeveroomWindowManager WindowManager => VsIdeScope.WindowManager;

    public IFileSystem FileSystem => VsIdeScope.FileSystem;

    public event EventHandler<EventArgs> WeakProjectsBuilt
    {
        add => VsIdeScope.WeakProjectsBuilt += value;
        remove => VsIdeScope.WeakProjectsBuilt -= value;
    }

    public event EventHandler<EventArgs> WeakProjectOutputsUpdated
    {
        add => VsIdeScope.WeakProjectOutputsUpdated += value;
        remove => VsIdeScope.WeakProjectOutputsUpdated -= value;
    }

    public void CalculateSourceLocationTrackingPositions(IEnumerable<SourceLocation> sourceLocations)
    {
        VsIdeScope.CalculateSourceLocationTrackingPositions(sourceLocations);
    }

    public bool GetTextBuffer(SourceLocation sourceLocation, out ITextBuffer textBuffer) =>
        VsIdeScope.GetTextBuffer(sourceLocation, out textBuffer);

    public SyntaxTree GetSyntaxTree(ITextBuffer textBuffer) => VsIdeScope.GetSyntaxTree(textBuffer);

    public Task RunOnBackgroundThread(Func<Task> action, Action<Exception> onException,
        [CallerMemberName] string callerName = "???") => VsIdeScope.RunOnBackgroundThread(action, onException);

    public Task RunOnUiThread(Action action) => VsIdeScope.RunOnUiThread(action);

    public void OpenIfNotOpened(string path)
    {
        VsIdeScope.OpenIfNotOpened(path);
    }

    public IProjectScope[] GetProjectsWithFeatureFiles() => VsIdeScope.GetProjectsWithFeatureFiles();

    public IDisposable CreateUndoContext(string undoLabel) => VsIdeScope.CreateUndoContext(undoLabel);

    public IServiceProvider ServiceProvider => VsIdeScope.ServiceProvider;
    public DTE Dte => VsIdeScope.Dte;
    public IDeveroomOutputPaneServices DeveroomOutputPaneServices => VsIdeScope.DeveroomOutputPaneServices;
    public IDeveroomErrorListServices DeveroomErrorListServices => VsIdeScope.DeveroomErrorListServices;

    public IProjectScope GetProjectScope(Project project) => VsIdeScope.GetProjectScope(project);

    public void Dispose()
    {
        VsIdeScope.Dispose();
    }

    #endregion
}
