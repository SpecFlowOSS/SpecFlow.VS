/**
TODO: Split  private ProjectBindingRegistry InvokeDiscovery()
 **/

namespace SpecFlow.VisualStudio.Discovery;

public class DiscoveryService : IDiscoveryService
{
    private readonly IDiscoveryResultProvider _discoveryResultProvider;
    private readonly IDeveroomErrorListServices _errorListServices;
    private readonly IDeveroomLogger _logger;
    private readonly IMonitoringService _monitoringService;
    public IProjectBindingRegistryCache BindingRegistryCache { get; }
    private readonly IProjectScope _projectScope;
    private readonly IProjectSettingsProvider _projectSettingsProvider;

    public DiscoveryService(IProjectScope projectScope, IDiscoveryResultProvider discoveryResultProvider, IProjectBindingRegistryCache bindingRegistryCacheCache)
    {
        _projectScope = projectScope;
        _discoveryResultProvider = discoveryResultProvider;
        _logger = _projectScope.IdeScope.Logger;
        _monitoringService = _projectScope.IdeScope.MonitoringService;
        _errorListServices = _projectScope.IdeScope.DeveroomErrorListServices;
        _projectSettingsProvider = _projectScope.GetProjectSettingsProvider();
        _projectSettingsProvider.WeakSettingsInitialized += ProjectSystemOnProjectsBuilt;
        _projectScope.IdeScope.WeakProjectOutputsUpdated += ProjectSystemOnProjectsBuilt;
        BindingRegistryCache = bindingRegistryCacheCache;
    }

    private IFileSystem FileSystem => _projectScope.IdeScope.FileSystem;

    public event EventHandler<EventArgs> WeakBindingRegistryChanged
    {
        add => WeakEventManager<IProjectBindingRegistryCache, EventArgs>.AddHandler(BindingRegistryCache, nameof(IProjectBindingRegistryCache.Changed), value);
        remove => WeakEventManager<IProjectBindingRegistryCache, EventArgs>.RemoveHandler(BindingRegistryCache, nameof(IProjectBindingRegistryCache.Changed),
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
        var currentHash = CreateProjectHash(projectSettings, testAssemblySource);

        return BindingRegistryCache.Value.ProjectHash == currentHash;
    }

    private int CreateProjectHash(ProjectSettings projectSetting, ConfigSource configSource)
    {
        return projectSetting.GetHashCode() ^ configSource.GetHashCode();
    }

    protected virtual ConfigSource GetTestAssemblySource(ProjectSettings projectSettings)
    {
        return projectSettings.IsSpecFlowTestProject
            ? ConfigSource.TryGetConfigSource(projectSettings.OutputAssemblyPath, FileSystem, _logger)
            : ConfigSource.Invalid;
    }

    private void TriggerDiscovery()
    {
        _projectScope.IdeScope.RunOnBackgroundThread(
            () => BindingRegistryCache.Update(InvokeDiscoveryWithTimer),
            _ => { });
    }

    private ProjectBindingRegistry InvokeDiscoveryWithTimer(ProjectBindingRegistry _)
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        var result = InvokeDiscovery();
        stopwatch.Stop();
        _logger.LogVerbose($"Discovery: {stopwatch.ElapsedMilliseconds} ms");
        return result;
    }

    private ProjectBindingRegistry InvokeDiscovery()
    {
        var projectSettings = _projectScope.GetProjectSettings();
        _errorListServices?.ClearErrors(DeveroomUserErrorCategory.Discovery);

        if (projectSettings.IsUninitialized)
        {
            _logger.LogVerbose("Uninitialized project settings");
            return ProjectBindingRegistry.Empty;
        }

        if (!projectSettings.IsSpecFlowTestProject)
        {
            _logger.LogVerbose("Non-SpecFlow test project");
            return ProjectBindingRegistry.Empty;
        }

        var testAssemblySource = GetTestAssemblySource(projectSettings);
        if (testAssemblySource == ConfigSource.Invalid)
        {
            var message =
                "Test assembly not found. Please build the project to enable the SpecFlow Visual Studio Extension features.";
            _logger.LogInfo(message);
            _errorListServices?.AddErrors(new[]
            {
                new DeveroomUserError
                {
                    Category = DeveroomUserErrorCategory.Discovery,
                    Message = message,
                    Type = TaskErrorCategory.Warning
                }
            });
            return ProjectBindingRegistry.Empty;
        }

        var result = _discoveryResultProvider.RunDiscovery(testAssemblySource.FilePath,
            projectSettings.SpecFlowConfigFilePath, projectSettings);

        if (result.IsFailed)
        {
            _logger.LogWarning(result.ErrorMessage);
            _logger.LogWarning(
                $"The project bindings (e.g. step definitions) could not be discovered. Navigation, step completion and other features are disabled. {Environment.NewLine}  Please check the error message above and report to https://github.com/SpecFlowOSS/SpecFlow.VS/issues if you cannot fix.");

            _errorListServices?.AddErrors(new[]
            {
                new DeveroomUserError
                {
                    Category = DeveroomUserErrorCategory.Discovery,
                    Message =
                        "The project bindings (e.g. step definitions) could not be discovered. Navigation, step completion and other features are disabled.",
                    Type = TaskErrorCategory.Warning
                }
            });
            return ProjectBindingRegistry.Empty;
        }

        var bindingImporter = new BindingImporter(result.SourceFiles, result.TypeNames, _logger);

        var stepDefinitions = result.StepDefinitions
            .Select(sd => bindingImporter.ImportStepDefinition(sd))
            .Where(psd => psd != null)
            .ToArray();
        var bindingRegistry =
            new ProjectBindingRegistry(stepDefinitions, CreateProjectHash(projectSettings, testAssemblySource));
        _logger.LogInfo(
            $"{bindingRegistry.StepDefinitions.Length} step definitions discovered for project {_projectScope.ProjectName}");

        if (bindingRegistry.StepDefinitions.Any(sd => !sd.IsValid))
        {
            _logger.LogWarning($"Invalid step definitions found: {Environment.NewLine}" +
                               string.Join(Environment.NewLine, bindingRegistry.StepDefinitions
                                   .Where(sd => !sd.IsValid)
                                   .Select(sd =>
                                       $"  {sd}: {sd.Error} at {sd.Implementation?.SourceLocation}")));

            _errorListServices?.AddErrors(
                bindingRegistry.StepDefinitions.Where(sd => !sd.IsValid)
                    .Select(sd => new DeveroomUserError
                    {
                        Category = DeveroomUserErrorCategory.Discovery,
                        Message = sd.Error,
                        SourceLocation = sd.Implementation?.SourceLocation,
                        Type = TaskErrorCategory.Error
                    })
            );
        }

        _monitoringService.MonitorSpecFlowDiscovery(bindingRegistry.StepDefinitions.IsEmpty, result.ErrorMessage,
            bindingRegistry.StepDefinitions.Length, projectSettings);

        return bindingRegistry;
    }
}
