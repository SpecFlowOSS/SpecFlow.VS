namespace SpecFlow.VisualStudio.Discovery;

public class DiscoveryService : IDiscoveryService
{
    private readonly IDiscoveryResultProvider _discoveryResultProvider;
    private readonly IDeveroomErrorListServices _errorListServices;
    private readonly IDeveroomLogger _logger;
    private readonly IMonitoringService _monitoringService;
    private readonly ProjectBindingRegistryContainer _projectBindingRegistryContainer;
    private readonly IProjectScope _projectScope;
    private readonly IProjectSettingsProvider _projectSettingsProvider;

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
        _projectBindingRegistryContainer = new ProjectBindingRegistryContainer(_projectScope.IdeScope);
        _projectBindingRegistryContainer.BindingRegistryChanged += TriggerBindingRegistryChanged;
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

    public ProjectBindingRegistry GetLastProcessedBindingRegistry()
    {
        return _projectBindingRegistryContainer.GetLastProcessedBindingRegistry();
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
        await _projectBindingRegistryContainer.UpdateBindingRegistry(update);
        Initialized.Set();
    }

    public Task<ProjectBindingRegistry> GetLatestBindingRegistry()
    {
        return _projectBindingRegistryContainer.GetLatestBindingRegistry();
    }

    public event EventHandler<EventArgs> BindingRegistryChanged;


    protected virtual void TriggerBindingRegistryChanged(object sender, EventArgs e)
    {
        BindingRegistryChanged?.Invoke(this, e);
    }

    private void ProjectSystemOnProjectsBuilt(object sender, EventArgs eventArgs)
    {
        _logger.LogVerbose("Projects built or settings initialized");
        CheckBindingRegistry();
    }

    private bool IsLastProcessedUpToDate()
    {
        if (!_projectBindingRegistryContainer.ProcessingTaskSucceed)
            return false;

        var projectSettings = _projectScope.GetProjectSettings();
        var testAssemblySource = GetTestAssemblySource(projectSettings);
        var currentHash = CreateProjectHash(projectSettings, testAssemblySource);

        return _projectBindingRegistryContainer.GetLastProcessedBindingRegistry().ProjectHash == currentHash;
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
            _ => Initialized.Set());
    }

    private ProjectBindingRegistry InvokeDiscoveryWithTimer(ProjectBindingRegistry _)
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        var result = InvokeDiscovery();
        Initialized.Set();
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
}
