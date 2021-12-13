namespace SpecFlow.VisualStudio.Discovery;

public class DiscoveryService : IDiscoveryService
{
    private readonly IDeveroomLogger _logger;
    private readonly IProjectScope _projectScope;
    private readonly IProjectSettingsProvider _projectSettingsProvider;
    private readonly DiscoveryInvoker _discoveryInvoker;

    public DiscoveryService(IProjectScope projectScope, IDiscoveryResultProvider discoveryResultProvider,
        IProjectBindingRegistryCache bindingRegistryCacheCache)
    {
        _projectScope = projectScope;
        _logger = _projectScope.IdeScope.Logger;
        _projectSettingsProvider = _projectScope.GetProjectSettingsProvider();
        _projectSettingsProvider.WeakSettingsInitialized += ProjectSystemOnProjectsBuilt;
        _projectScope.IdeScope.WeakProjectOutputsUpdated += ProjectSystemOnProjectsBuilt;
        BindingRegistryCache = bindingRegistryCacheCache;
        _discoveryInvoker = new DiscoveryInvoker(
            _logger,
            _projectScope.IdeScope.DeveroomErrorListServices, 
            _projectScope,
            discoveryResultProvider,
            _projectScope.IdeScope.MonitoringService, GetTestAssemblySource);
    }

    private IFileSystem FileSystem => _projectScope.IdeScope.FileSystem;
    public IProjectBindingRegistryCache BindingRegistryCache { get; }

    public event EventHandler<EventArgs> WeakBindingRegistryChanged
    {
        add => WeakEventManager<IProjectBindingRegistryCache, EventArgs>.AddHandler(BindingRegistryCache,
            nameof(IProjectBindingRegistryCache.Changed), value);
        remove => WeakEventManager<IProjectBindingRegistryCache, EventArgs>.RemoveHandler(BindingRegistryCache,
            nameof(IProjectBindingRegistryCache.Changed),
            value);
    }

    public void InitializeBindingRegistry()
    {
        _logger.LogVerbose("Initial discovery triggered...");
        TriggerDiscovery();
    }

    public void CheckBindingRegistry()
    {
        if (BindingRegistryCache.Processing)
            return;

        if (BindingRegistryCache.Value.IsPatched)
            return;

        if (IsCacheUpToDate())
            return;

        TriggerDiscovery();
    }

    public void Dispose()
    {
        _projectScope.IdeScope.WeakProjectsBuilt -= ProjectSystemOnProjectsBuilt;
        _projectSettingsProvider.SettingsInitialized -= ProjectSystemOnProjectsBuilt;
    }

    private void ProjectSystemOnProjectsBuilt(object sender, EventArgs eventArgs)
    {
        _logger.LogVerbose("Projects built or settings initialized");
        CheckBindingRegistry();
    }

    private bool IsCacheUpToDate()
    {
        var projectSettings = _projectScope.GetProjectSettings();
        var testAssemblySource = GetTestAssemblySource(projectSettings);
        var currentHash = DiscoveryInvoker.CreateProjectHash(projectSettings, testAssemblySource);

        return BindingRegistryCache.Value.ProjectHash == currentHash;
    }

    protected virtual ConfigSource GetTestAssemblySource(ProjectSettings projectSettings) =>
        projectSettings.IsSpecFlowTestProject
            ? ConfigSource.TryGetConfigSource(projectSettings.OutputAssemblyPath, FileSystem, _logger)
            : ConfigSource.Invalid;

    private void TriggerDiscovery()
    {
        _projectScope.IdeScope.RunOnBackgroundThread(
            () => BindingRegistryCache.Update(_discoveryInvoker.InvokeDiscoveryWithTimer),
            _ => { });
    }
}