namespace SpecFlow.VisualStudio.Discovery;

public class DiscoveryService : IDiscoveryService
{
    private readonly IDiscoveryResultProvider _discoveryResultProvider;
    private readonly IDeveroomErrorListServices _errorListServices;
    private readonly IDeveroomLogger _logger;
    private readonly IMonitoringService _monitoringService;
    private readonly IProjectScope _projectScope;
    private readonly IProjectSettingsProvider _projectSettingsProvider;
    private ProjectBindingRegistryCache _cached = new ProjectBindingRegistryCacheUninitialized();
    private ProjectBindingRegistry _cache;

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

        _cache = ProjectBindingRegistry.Empty;
        _currentBindingRegistrySource = new TaskCompletionSource<ProjectBindingRegistry>();
        _currentBindingRegistrySource.SetResult(_cache);
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
        if (IsCacheUpToDate())
            return;

        TriggerDiscovery();
    }

    public ProjectBindingRegistry GetBindingRegistry()
    {
        return _cache;
    }

    public void Dispose()
    {
        _projectScope.IdeScope.WeakProjectsBuilt -= ProjectSystemOnProjectsBuilt;
        _projectSettingsProvider.SettingsInitialized -= ProjectSystemOnProjectsBuilt;
    }

    public async Task ProcessAsync(CSharpStepDefinitionFile stepDefinitionFile)
    {
        _logger.LogVerbose("ProcessAsync started");
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
            _logger.LogVerbose($"current reg v{bindingRegistry.Version} has {bindingRegistry.StepDefinitions.Length}");
            if (bindingRegistry.IsFailed)
                throw
                    new Exception(
                        "changing invalid registry"); //TODO: do not try to modify an invalid registry, return here without doing anything. The exception throw is to better detect async issues only.
            bindingRegistry = bindingRegistry
                .Where(binding =>
                    binding.Implementation.SourceLocation.SourceFile != stepDefinitionFile.StepDefinitionPath)
                .WithStepDefinitions(projectStepDefinitionBindings);
            _logger.LogVerbose(
                $"replacing with reg v{bindingRegistry.Version} has {bindingRegistry.StepDefinitions.Length}");

            _cached = _cached.WithBindingRegistry(bindingRegistry);

            return bindingRegistry;
        });
    }

    public event EventHandler<EventArgs> BindingRegistryChanged;

    private void ProjectSystemOnProjectsBuilt(object sender, EventArgs eventArgs)
    {
        _logger.LogVerbose("Projects built or settings initialized");
        CheckBindingRegistry();
    }

    private bool IsCacheUpToDate()
    {
        var projectSettings = _projectScope.GetProjectSettings();
        var testAssemblySource = GetTestAssemblySource(projectSettings);

        return _cached.IsUpToDate(projectSettings, testAssemblySource.LastChangeTime);
    }

    protected virtual ConfigSource GetTestAssemblySource(ProjectSettings projectSettings)
    {
        return projectSettings.IsSpecFlowTestProject
            ? ConfigSource.TryGetConfigSource(projectSettings.OutputAssemblyPath, FileSystem, _logger)
            : ConfigSource.Invalid;
    }

    private void TriggerDiscovery()
    {
        var projectSettings = _projectScope.GetProjectSettings();
        _errorListServices?.ClearErrors(DeveroomUserErrorCategory.Discovery);
        ProjectBindingRegistryCache skippedResult =
            GetSkippedBindingRegistryResult(projectSettings, out var testAssemblySource);
        if (skippedResult != null)
        {
            _cached = skippedResult;
            //PublishBindingRegistryResult(skippedResult);
            Initialized.Set();
            return;
        }

        TriggerDiscoveryOnBackgroundThread(projectSettings, testAssemblySource);
    }

    private ProjectBindingRegistryCache GetSkippedBindingRegistryResult(ProjectSettings projectSettings,
        out ConfigSource testAssemblySource)
    {
        testAssemblySource = null;

        if (projectSettings.IsUninitialized)
        {
            _logger.LogVerbose("Uninitialized project settings");
            return new ProjectBindingRegistryCacheUninitializedProjectSettings();
        }

        if (!projectSettings.IsSpecFlowTestProject)
        {
            _logger.LogVerbose("Non-SpecFlow test project");
            if (_cached is ProjectBindingRegistryCacheUninitialized)
                _logger.LogWarning(
                    $"Could not detect the SpecFlow version of the project that is required for navigation, step completion and other features. {Environment.NewLine}  Currently only NuGet package referenced can be detected. Please check https://github.com/specsolutions/deveroom-visualstudio/issues/14 for details.");
            return new ProjectBindingRegistryCacheNonSpecFlowTestProject();
        }

        testAssemblySource = GetTestAssemblySource(projectSettings);
        if (testAssemblySource == null)
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
            return new ProjectBindingRegistryCacheTestAssemblyNotFound();
        }

        return null;
    }

    private void TriggerDiscoveryOnBackgroundThread(ProjectSettings projectSettings, ConfigSource testAssemblySource)
    {
        _projectScope.IdeScope.RunOnBackgroundThread(()=>UpdateBindingRegistry(_ =>
        {
            var newRegistry = InvokeDiscoveryWithTimer(projectSettings, testAssemblySource);
            return newRegistry.BindingRegistry;
        }), _ => {Initialized.Set(); _cache= ProjectBindingRegistry.Empty;});
    }

    private ProjectBindingRegistryCache InvokeDiscoveryWithTimer(ProjectSettings projectSettings,
        ConfigSource testAssemblySource)
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        var result = InvokeDiscovery(projectSettings, testAssemblySource);
        stopwatch.Stop();
        if (result.IsDiscovered)
            _logger.LogVerbose($"Discovery: {stopwatch.ElapsedMilliseconds} ms");
        return result;
    }

    private ProjectBindingRegistryCache InvokeDiscovery(ProjectSettings projectSettings,
        ConfigSource testAssemblySource)
    {
        try
        {
            var result = _discoveryResultProvider.RunDiscovery(testAssemblySource.FilePath,
                projectSettings.SpecFlowConfigFilePath, projectSettings);
            ProjectBindingRegistry bindingRegistry = null;
            if (result.IsFailed)
            {
                bindingRegistry = ProjectBindingRegistry.Empty;
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
            }
            else
            {
                var bindingImporter = new BindingImporter(result.SourceFiles, result.TypeNames, _logger);

                var stepDefinitions = result.StepDefinitions
                    .Select(sd => bindingImporter.ImportStepDefinition(sd))
                    .Where(psd => psd != null)
                    .ToArray();
                bindingRegistry = new ProjectBindingRegistry(stepDefinitions);
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
            }

            _monitoringService.MonitorSpecFlowDiscovery(bindingRegistry.IsFailed, result.ErrorMessage,
                bindingRegistry.StepDefinitions.Length, projectSettings);
            return new ProjectBindingRegistryCacheDiscovered(bindingRegistry, projectSettings,
                testAssemblySource.LastChangeTime);
        }
        catch (Exception ex)
        {
            _logger.LogException(_monitoringService, ex);
            return new ProjectBindingRegistryCacheError();
        }
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

    private TaskCompletionSource<ProjectBindingRegistry> _currentBindingRegistrySource;

    public async Task UpdateBindingRegistry(Func<ProjectBindingRegistry, ProjectBindingRegistry> update)
    {
        var n = 0;
        do
        {
            ++n;
            var currentSource = _currentBindingRegistrySource;
            var newRegistrySource = new TaskCompletionSource<ProjectBindingRegistry>();
            
            var originalSource =
                Interlocked.CompareExchange(ref _currentBindingRegistrySource, newRegistrySource, currentSource);

          //  _logger.LogVerbose($"Wait {n} c:{currentSource.Task.Id}-{currentSource.Task.Status} n:{newRegistrySource.Task.Id}-{newRegistrySource.Task.Status} o:{originalSource.Task.Id}-{originalSource.Task.Status} _:{_currentBindingRegistrySource.Task.Id}-{_currentBindingRegistrySource.Task.Status} ");
            var registry = await WaitForCompletion(originalSource.Task);

            if (ReferenceEquals(originalSource, currentSource))
            {
                _logger.LogVerbose(
                    $"Process {n} c:{currentSource.Task.Id}-{currentSource.Task.Status} n:{newRegistrySource.Task.Id}-{newRegistrySource.Task.Status} o:{originalSource.Task.Id}-{originalSource.Task.Status} _:{_currentBindingRegistrySource.Task.Id}-{_currentBindingRegistrySource.Task.Status} ");

                var updatedRegistry = update(registry);
                if (updatedRegistry.Version == registry.Version)
                {
                    newRegistrySource.SetResult(updatedRegistry);
                    return;
                }

                if (updatedRegistry.Version < registry.Version)
                    throw new InvalidOperationException(
                        $"Cannot downgrade bindingRegistry from V{registry.Version} to V{updatedRegistry.Version}");
                if (updatedRegistry.IsFailed)
                    throw new InvalidOperationException($"Update failure in bindingRegistry V{registry.Version}");
                CalculateSourceLocationTrackingPositions(updatedRegistry);
                _logger.LogVerbose(
                    $"Done {n} r:{updatedRegistry.Version} c:{currentSource.Task.Id}-{currentSource.Task.Status} n:{newRegistrySource.Task.Id}-{newRegistrySource.Task.Status} o:{originalSource.Task.Id}-{originalSource.Task.Status} _:{_currentBindingRegistrySource.Task.Id}-{_currentBindingRegistrySource.Task.Status} ");

                newRegistrySource.SetResult(updatedRegistry);
                _cache = updatedRegistry;  
                DisposeSourceLocationTrackingPositions(registry);
                TriggerBindingRegistryChanged();
                Initialized.Set();

                return;
            }

            _logger.LogVerbose(
                $"Retry {n} c:{currentSource.Task.Id}-{currentSource.Task.Status} n:{newRegistrySource.Task.Id}-{newRegistrySource.Task.Status} o:{originalSource.Task.Id}-{originalSource.Task.Status} _:{_currentBindingRegistrySource.Task.Id}-{_currentBindingRegistrySource.Task.Status} ");
        } while (true);
    }

    public Task<ProjectBindingRegistry> GetLatestBindingRegistry()
    {
        return WaitForCompletion(_currentBindingRegistrySource.Task);
    }

    private async Task<ProjectBindingRegistry> WaitForCompletion(Task<ProjectBindingRegistry> task)
    {
        CancellationTokenSource cts = Debugger.IsAttached
            ? new CancellationTokenSource(TimeSpan.FromMinutes(1))
            : new CancellationTokenSource(TimeSpan.FromSeconds(15));

        _logger.LogVerbose($"task:{task.Id}-{task.Status}");
        var timeoutTask = Task.Delay(-1, cts.Token);
        var result = await Task.WhenAny(task, timeoutTask);
        if (ReferenceEquals(result, timeoutTask))
            throw new TimeoutException("Binding registry in not processed in time");

        var projectBindingRegistry = await (result as Task<ProjectBindingRegistry>)!;
        _logger.LogVerbose($"{projectBindingRegistry.Version}");
        return projectBindingRegistry;
    }
}
