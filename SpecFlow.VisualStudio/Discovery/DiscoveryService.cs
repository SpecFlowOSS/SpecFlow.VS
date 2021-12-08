namespace SpecFlow.VisualStudio.Discovery;

public class DiscoveryService : IDiscoveryService
{
    private readonly IDiscoveryResultProvider _discoveryResultProvider;
    private readonly IDeveroomErrorListServices _errorListServices;
    private readonly IDeveroomLogger _logger;
    private readonly IMonitoringService _monitoringService;
    private readonly IProjectScope _projectScope;
    private readonly IProjectSettingsProvider _projectSettingsProvider;
    private ProjectBindingRegistry _lastProcessedBindingRegistry;
    private TaskCompletionSource<ProjectBindingRegistry> _upToDateBindingRegistrySource;

    public DiscoveryService(IProjectScope projectScope, IDiscoveryResultProvider discoveryResultProvider = null)
    {
        _projectScope = projectScope;
        _discoveryResultProvider = discoveryResultProvider ?? new DiscoveryResultProvider(_projectScope);
        _logger = _projectScope.IdeScope.Logger;
        _monitoringService = _projectScope.IdeScope.MonitoringService;
        _errorListServices = _projectScope.IdeScope.DeveroomErrorListServices;
        _projectSettingsProvider = _projectScope.GetProjectSettingsProvider();
        _projectSettingsProvider.WeakSettingsInitialized += ProjectSystemOnProjectsBuilt;
        _projectScope.IdeScope.WeakProjectOutputsUpdated += ProjectSystemOnProjectsBuilt;

        _lastProcessedBindingRegistry = ProjectBindingRegistry.Empty;
        _upToDateBindingRegistrySource = new TaskCompletionSource<ProjectBindingRegistry>();
        _upToDateBindingRegistrySource.SetResult(_lastProcessedBindingRegistry);
    }

    private IFileSystem FileSystem => _projectScope.IdeScope.FileSystem;

    public event EventHandler<EventArgs> WeakBindingRegistryChanged
    {
        add => WeakEventManager<DiscoveryService, EventArgs>.AddHandler(this, nameof(BindingRegistryChanged), value);
        remove => WeakEventManager<DiscoveryService, EventArgs>.RemoveHandler(this, nameof(BindingRegistryChanged),
            value);
    }

    public ManualResetEvent Initialized { get; } = new(false);

    public void InitializeBindingRegistry()
    {
        _logger.LogVerbose("Initial discovery triggered...");
        TriggerDiscovery();
    }

    public void CheckBindingRegistry()
    {
        if (IsLastProcessedUpToDate())
            return;

        TriggerDiscovery();
    }

    public ProjectBindingRegistry GetBindingRegistry()
    {
        return _lastProcessedBindingRegistry;
    }

    public void Dispose()
    {
        _projectScope.IdeScope.WeakProjectsBuilt -= ProjectSystemOnProjectsBuilt;
        _projectSettingsProvider.SettingsInitialized -= ProjectSystemOnProjectsBuilt;
    }

    public async Task ProcessAsync(CSharpStepDefinitionFile stepDefinitionFile)
    {
        SyntaxTree tree = CSharpSyntaxTree.ParseText(stepDefinitionFile.Content);
        var rootNode = await tree.GetRootAsync();

        var allMethods = rootNode
            .DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .ToArray();

        var projectStepDefinitionBindings = new List<ProjectStepDefinitionBinding>(allMethods.Length);
        foreach (MethodDeclarationSyntax method in allMethods)
        {
            var attributes = RenameStepStepDefinitionClassAction.GetAttributesWithTokens(method);

            var methodBodyBeginToken = method.Body.GetFirstToken();
            var methodBodyBeginPosition = methodBodyBeginToken.GetLocation().GetLineSpan().StartLinePosition;
            var methodBodyEndToken = method.Body.GetLastToken();
            var methodBodyEndPosition = methodBodyEndToken.GetLocation().GetLineSpan().StartLinePosition;

            Scope scope = null;
            var parameterTypes = method.ParameterList.Parameters
                .Select(p => p.Type.ToString())
                .ToArray();

            var sourceLocation = new SourceLocation(stepDefinitionFile.StepDefinitionPath,
                methodBodyBeginPosition.Line + 1,
                methodBodyBeginPosition.Character + 1,
                methodBodyEndPosition.Line + 1,
                methodBodyEndPosition.Character + 1);
            var implementation =
                new ProjectStepDefinitionImplementation(FullMethodName(method), parameterTypes, sourceLocation);

            foreach (var (attribute, token) in attributes)
            {
                var stepDefinitionType = (ScenarioBlock) Enum.Parse(typeof(ScenarioBlock), attribute.Name.ToString());
                var regex = new Regex($"^{token.ValueText}$");

                var stepDefinitionBinding = new ProjectStepDefinitionBinding(stepDefinitionType, regex, scope,
                    implementation, token.ValueText);

                projectStepDefinitionBindings.Add(stepDefinitionBinding);
            }
        }

        _logger.LogVerbose($"ProcessAsync found {projectStepDefinitionBindings.Count} stepdefs");

        await UpdateBindingRegistry(bindingRegistry =>
        {
            bindingRegistry = bindingRegistry
                .Where(binding =>
                    binding.Implementation.SourceLocation.SourceFile != stepDefinitionFile.StepDefinitionPath)
                .WithStepDefinitions(projectStepDefinitionBindings);

            return bindingRegistry;
        });
    }

    public async Task UpdateBindingRegistry(Func<ProjectBindingRegistry, ProjectBindingRegistry> update)
    {
        var iteration = 0;
        TaskCompletionSource<ProjectBindingRegistry> comparandSource;
        TaskCompletionSource<ProjectBindingRegistry> originalSource;
        TaskCompletionSource<ProjectBindingRegistry> newRegistrySource;
        ProjectBindingRegistry originalRegistry;
        do
        {
            ++iteration;
            comparandSource = _upToDateBindingRegistrySource;
            newRegistrySource = new TaskCompletionSource<ProjectBindingRegistry>();

            originalSource =
                Interlocked.CompareExchange(ref _upToDateBindingRegistrySource, newRegistrySource, comparandSource);

            originalRegistry = await WaitForCompletion(originalSource.Task);
        } while (!ReferenceEquals(originalSource, comparandSource));

        var updatedRegistry = update(originalRegistry);
        if (updatedRegistry.Version == originalRegistry.Version)
        {
            newRegistrySource.SetResult(updatedRegistry);
            return;
        }

        if (updatedRegistry.Version < originalRegistry.Version)
            throw new InvalidOperationException(
                $"Cannot downgrade bindingRegistry from V{originalRegistry.Version} to V{updatedRegistry.Version}");
        CalculateSourceLocationTrackingPositions(updatedRegistry);

        _lastProcessedBindingRegistry = updatedRegistry;
        newRegistrySource.SetResult(updatedRegistry);
        TriggerBindingRegistryChanged();
        Initialized.Set();
        _logger.LogVerbose(
            $"BindingRegistry is modified {originalRegistry}->{updatedRegistry}. Iteration:{iteration}");
        DisposeSourceLocationTrackingPositions(originalRegistry);
    }

    public Task<ProjectBindingRegistry> GetLatestBindingRegistry()
    {
        return WaitForCompletion(_upToDateBindingRegistrySource.Task);
    }

    public event EventHandler<EventArgs> BindingRegistryChanged;

    private void ProjectSystemOnProjectsBuilt(object sender, EventArgs eventArgs)
    {
        _logger.LogVerbose("Projects built or settings initialized");
        CheckBindingRegistry();
    }

    private bool IsLastProcessedUpToDate()
    {
        if (_upToDateBindingRegistrySource.Task.Status != TaskStatus.RanToCompletion)
            return false;

        var projectSettings = _projectScope.GetProjectSettings();
        var testAssemblySource = GetTestAssemblySource(projectSettings);
        var currentHash = CreateProjectHash(projectSettings, testAssemblySource);

        return _lastProcessedBindingRegistry.ProjectHash == currentHash;
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
            () => UpdateBindingRegistry(InvokeDiscoveryWithTimer),
            _ =>
            {
                Initialized.Set();
                _lastProcessedBindingRegistry = ProjectBindingRegistry.Empty;
            });
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

        CalculateSourceLocationTrackingPositions(bindingRegistry);

        _monitoringService.MonitorSpecFlowDiscovery(bindingRegistry.StepDefinitions.IsEmpty, result.ErrorMessage,
            bindingRegistry.StepDefinitions.Length, projectSettings);

        return bindingRegistry;
    }

    protected virtual void TriggerBindingRegistryChanged()
    {
        BindingRegistryChanged?.Invoke(this, EventArgs.Empty);
    }

    private void CalculateSourceLocationTrackingPositions(ProjectBindingRegistry bindingRegistry)
    {
        var sourceLocations = bindingRegistry.StepDefinitions.Select(sd => sd.Implementation.SourceLocation);
        _projectScope.IdeScope.CalculateSourceLocationTrackingPositions(sourceLocations);
    }

    private void DisposeSourceLocationTrackingPositions(ProjectBindingRegistry bindingRegistry)
    {
        if (bindingRegistry == null)
            return;
        foreach (var sourceLocation in bindingRegistry.StepDefinitions.Select(sd => sd.Implementation.SourceLocation)
                     .Where(sl => sl?.SourceLocationSpan != null))
        {
            sourceLocation.SourceLocationSpan.Dispose();
            sourceLocation.SourceLocationSpan = null;
        }

        _logger.LogVerbose($"Tracking positions disposed on V{bindingRegistry.Version}");
    }

    private static string FullMethodName(MethodDeclarationSyntax method)
    {
        StringBuilder sb = new StringBuilder();
        var containingClass = method.Parent as ClassDeclarationSyntax;
        if (containingClass.Parent is BaseNamespaceDeclarationSyntax namespaceSyntax)
        {
            var containingNamespace = namespaceSyntax.Name;
            sb.Append(containingNamespace).Append('.');
        }

        sb.Append(containingClass.Identifier.Text).Append('.').Append(method.Identifier.Text);
        return sb.ToString();
    }

    private async Task<ProjectBindingRegistry> WaitForCompletion(Task<ProjectBindingRegistry> task)
    {
        CancellationTokenSource cts = Debugger.IsAttached
            ? new CancellationTokenSource(TimeSpan.FromMinutes(1))
            : new CancellationTokenSource(TimeSpan.FromSeconds(15));

        var timeoutTask = Task.Delay(-1, cts.Token);
        var result = await Task.WhenAny(task, timeoutTask);
        if (ReferenceEquals(result, timeoutTask))
            throw new TimeoutException("Binding registry in not processed in time");

        var projectBindingRegistry = await (result as Task<ProjectBindingRegistry>)!;
        return projectBindingRegistry;
    }
}
