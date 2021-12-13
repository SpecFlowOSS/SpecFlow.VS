namespace SpecFlow.VisualStudio.Discovery;

internal class DiscoveryInvoker
{
    private readonly IDeveroomLogger _logger;
    private readonly IDeveroomErrorListServices _errorListServices;
    private readonly IProjectScope _projectScope;
    private readonly IDiscoveryResultProvider _discoveryResultProvider;
    private readonly IMonitoringService _monitoringService;
    private readonly Func<ProjectSettings, ConfigSource> _getTestAssemblySource;

    public DiscoveryInvoker(IDeveroomLogger logger, IDeveroomErrorListServices errorListServices, IProjectScope projectScope, IDiscoveryResultProvider discoveryResultProvider, IMonitoringService monitoringService, Func<ProjectSettings, ConfigSource> getTestAssemblySource)
    {
        _logger = logger;
        _errorListServices = errorListServices;
        _projectScope = projectScope;
        _discoveryResultProvider = discoveryResultProvider;
        _monitoringService = monitoringService;
        _getTestAssemblySource = getTestAssemblySource;
    }

    public static int CreateProjectHash(ProjectSettings projectSetting, ConfigSource configSource) =>
        projectSetting.GetHashCode() ^ configSource.GetHashCode();

    public ProjectBindingRegistry InvokeDiscoveryWithTimer(ProjectBindingRegistry _)
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        var bindingRegistry = new SuccessDiscovery(_logger, _errorListServices)
            .WhenProjectSettingsIsInitialized(_projectScope.GetProjectSettings())
            .AndProjectIsSpecFlowProject()
            .AndConfigSourceIsValid(_getTestAssemblySource)
            .AndDiscoveryProviderSucceed(_discoveryResultProvider)
            .ThenImportStepDefinitions(_projectScope.ProjectName)
            .AndCreateBindingRegistry(_monitoringService);
        stopwatch.Stop();

        _logger.LogVerbose($"{bindingRegistry.StepDefinitions.Length} step definitions discovered in {stopwatch.Elapsed}");
        return bindingRegistry;
    }

    private class SuccessDiscovery : IDiscovery
    {
        private readonly IDeveroomErrorListServices _errorListServices;
        private readonly IDeveroomLogger _logger;
        private DiscoveryResult _discoveryResult;
        private ProjectSettings _projectSettings;
        private ImmutableArray<ProjectStepDefinitionBinding> _stepDefinitions;
        private ConfigSource _testAssemblySource;

        public SuccessDiscovery(IDeveroomLogger logger, IDeveroomErrorListServices errorListServices)
        {
            _logger = logger;
            _errorListServices = errorListServices;
            _errorListServices.ClearErrors(DeveroomUserErrorCategory.Discovery);
        }

        public IDiscovery AndProjectIsSpecFlowProject()
        {
            if (_projectSettings.IsSpecFlowTestProject)
                return this;

            _logger.LogVerbose("Non-SpecFlow test project");
            return new FailedDiscovery();
        }

        public IDiscovery AndConfigSourceIsValid(Func<ProjectSettings, ConfigSource> getTestAssemblySource)
        {
            _testAssemblySource = getTestAssemblySource(_projectSettings);
            if (_testAssemblySource != ConfigSource.Invalid)
                return this;

            var message =
                "Test assembly not found. Please build the project to enable the SpecFlow Visual Studio Extension features.";
            _logger.LogInfo(message);
            _errorListServices.AddErrors(new[]
            {
                new DeveroomUserError
                {
                    Category = DeveroomUserErrorCategory.Discovery,
                    Message = message,
                    Type = TaskErrorCategory.Warning
                }
            });
            return new FailedDiscovery();
        }

        public IDiscovery AndDiscoveryProviderSucceed(IDiscoveryResultProvider discoveryResultProvider)
        {
            _discoveryResult = discoveryResultProvider.RunDiscovery(_testAssemblySource.FilePath,
                _projectSettings.SpecFlowConfigFilePath, _projectSettings);

            if (!_discoveryResult.IsFailed)
                return this;

            _logger.LogWarning(_discoveryResult.ErrorMessage);
            _logger.LogWarning(
                "The project bindings (e.g. step definitions) could not be discovered." +
                " Navigation, step completion and other features are disabled. " + Environment.NewLine +
                "  Please check the error message above and report to https://github.com/SpecFlowOSS/SpecFlow.VS/issues if you cannot fix.");

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
            return new FailedDiscovery();
        }

        public IDiscovery ThenImportStepDefinitions(string projectName)
        {
            var bindingImporter =
                new BindingImporter(_discoveryResult.SourceFiles, _discoveryResult.TypeNames, _logger);

            _stepDefinitions = _discoveryResult.StepDefinitions
                .Select(sd => bindingImporter.ImportStepDefinition(sd))
                .Where(psd => psd != null)
                .ToImmutableArray();

            _logger.LogInfo(
                $"{_stepDefinitions.Length} step definitions discovered for project {projectName}");

            ReportInvalidStepDefinitions();

            return this;
        }

        public ProjectBindingRegistry AndCreateBindingRegistry(IMonitoringService monitoringService)
        {
            monitoringService.MonitorSpecFlowDiscovery(_stepDefinitions.IsEmpty, _discoveryResult.ErrorMessage,
                _stepDefinitions.Length, _projectSettings);

            var bindingRegistry =
                new ProjectBindingRegistry(_stepDefinitions, CreateProjectHash(_projectSettings, _testAssemblySource));
            return bindingRegistry;
        }

        public IDiscovery WhenProjectSettingsIsInitialized(ProjectSettings projectSettings)
        {
            _projectSettings = projectSettings;
            if (projectSettings.IsUninitialized)
            {
                _logger.LogVerbose("Uninitialized project settings");
                return new FailedDiscovery();
            }

            return this;
        }

        private void ReportInvalidStepDefinitions()
        {
            if (!_stepDefinitions.Any(sd => !sd.IsValid))
                return;

            _logger.LogWarning($"Invalid step definitions found: {Environment.NewLine}" +
                               string.Join(Environment.NewLine, _stepDefinitions
                                   .Where(sd => !sd.IsValid)
                                   .Select(sd =>
                                       $"  {sd}: {sd.Error} at {sd.Implementation?.SourceLocation}")));

            _errorListServices.AddErrors(
                _stepDefinitions.Where(sd => !sd.IsValid)
                    .Select(sd => new DeveroomUserError
                    {
                        Category = DeveroomUserErrorCategory.Discovery,
                        Message = sd.Error,
                        SourceLocation = sd.Implementation?.SourceLocation,
                        Type = TaskErrorCategory.Error
                    })
            );
        }
    }

    private class FailedDiscovery : IDiscovery
    {
        public IDiscovery AndProjectIsSpecFlowProject() => this;
        public IDiscovery AndConfigSourceIsValid(Func<ProjectSettings, ConfigSource> getTestAssemblySource) => this;
        public IDiscovery AndDiscoveryProviderSucceed(IDiscoveryResultProvider discoveryResultProvider) => this;
        public IDiscovery ThenImportStepDefinitions(string projectName) => this;

        public ProjectBindingRegistry AndCreateBindingRegistry(IMonitoringService monitoringService) =>
            ProjectBindingRegistry.Empty;
    }

    private interface IDiscovery
    {
        IDiscovery AndProjectIsSpecFlowProject();
        IDiscovery AndConfigSourceIsValid(Func<ProjectSettings, ConfigSource> getTestAssemblySource);
        IDiscovery AndDiscoveryProviderSucceed(IDiscoveryResultProvider discoveryResultProvider);
        IDiscovery ThenImportStepDefinitions(string projectName);
        ProjectBindingRegistry AndCreateBindingRegistry(IMonitoringService monitoringService);
    }
}
