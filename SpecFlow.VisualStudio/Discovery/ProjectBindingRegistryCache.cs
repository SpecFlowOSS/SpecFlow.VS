namespace SpecFlow.VisualStudio.Discovery;

public class ProjectBindingRegistryCache : IProjectBindingRegistryCache
{
    private readonly IIdeScope _ideScope;
    private readonly IDeveroomLogger _logger;
    private TaskCompletionSource<ProjectBindingRegistry> _upToDateBindingRegistrySource;

    public ProjectBindingRegistryCache(IIdeScope ideScope)
    {
        _ideScope = ideScope;
        _logger = ideScope.Logger;

        Value = ProjectBindingRegistry.Empty;
        _upToDateBindingRegistrySource = new TaskCompletionSource<ProjectBindingRegistry>();
        _upToDateBindingRegistrySource.SetResult(Value);
    }

    public ProjectBindingRegistry Value { get; private set; }

    public bool Processing => _upToDateBindingRegistrySource.Task.Status != TaskStatus.RanToCompletion;

    public event EventHandler<EventArgs> Changed;

    public async Task Update(Func<ProjectBindingRegistry, ProjectBindingRegistry> updateFunc)
    {
        (TaskCompletionSource<ProjectBindingRegistry> newRegistrySource, ProjectBindingRegistry originalRegistry) =
            await GetThreadSafeRegistry();

        var updatedRegistry = InvokeUpdateFunc(updateFunc, originalRegistry, newRegistrySource);
        if (updatedRegistry.Version == originalRegistry.Version)
            return;

        CalculateSourceLocationTrackingPositions(updatedRegistry);

        Value = updatedRegistry;
        newRegistrySource.SetResult(updatedRegistry);
        Changed?.Invoke(this, EventArgs.Empty);
        _logger.LogVerbose(
            $"BindingRegistryCache is modified {originalRegistry}->{updatedRegistry}.");
        DisposeSourceLocationTrackingPositions(originalRegistry);
    }

    public Task<ProjectBindingRegistry> GetLatest()
    {
        return WaitForCompletion(_upToDateBindingRegistrySource.Task);
    }

    private async
        Task<(TaskCompletionSource<ProjectBindingRegistry> newRegistrySource, ProjectBindingRegistry originalRegistry)>
        GetThreadSafeRegistry()
    {
        TaskCompletionSource<ProjectBindingRegistry> comparandSource;
        TaskCompletionSource<ProjectBindingRegistry> originalSource;
        TaskCompletionSource<ProjectBindingRegistry> newRegistrySource;
        ProjectBindingRegistry originalRegistry;
        var iteration = 0;
        do
        {
            iteration++;
            comparandSource = _upToDateBindingRegistrySource;
            newRegistrySource = new TaskCompletionSource<ProjectBindingRegistry>();

            originalSource =
                Interlocked.CompareExchange(ref _upToDateBindingRegistrySource, newRegistrySource, comparandSource);

            originalRegistry = await WaitForCompletion(originalSource.Task);
        } while (!ReferenceEquals(originalSource, comparandSource));

        _logger.LogVerbose($"Got access to {originalRegistry} in {iteration} iteration(s)");

        return (newRegistrySource, originalRegistry);
    }

    private ProjectBindingRegistry InvokeUpdateFunc(Func<ProjectBindingRegistry, ProjectBindingRegistry> update,
        ProjectBindingRegistry originalRegistry,
        TaskCompletionSource<ProjectBindingRegistry> newRegistrySource)
    {
        var updatedRegistry = update(originalRegistry);

        if (updatedRegistry.Version == originalRegistry.Version)
        {
            newRegistrySource.SetResult(updatedRegistry);
            return updatedRegistry;
        }

        if (updatedRegistry.Version < originalRegistry.Version)
        {
            DisposeSourceLocationTrackingPositions(updatedRegistry);
            throw new InvalidOperationException(
                $"Cannot downgrade bindingRegistry from V{originalRegistry.Version} to V{updatedRegistry.Version}");
        }

        return updatedRegistry;
    }

    private async Task<ProjectBindingRegistry> WaitForCompletion(Task<ProjectBindingRegistry> task)
    {
        CancellationTokenSource cts = Debugger.IsAttached
            ? new CancellationTokenSource(TimeSpan.FromSeconds(60))
            : new CancellationTokenSource(TimeSpan.FromSeconds(15));

        var timeoutTask = Task.Delay(-1, cts.Token);
        var result = await Task.WhenAny(task, timeoutTask);
        if (ReferenceEquals(result, timeoutTask))
            throw new TimeoutException("Binding registry in not processed in time");

        return await task;
    }

    private void CalculateSourceLocationTrackingPositions(ProjectBindingRegistry bindingRegistry)
    {
        var sourceLocations = bindingRegistry.StepDefinitions.Select(sd => sd.Implementation.SourceLocation);
        _ideScope.CalculateSourceLocationTrackingPositions(sourceLocations);
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
}
