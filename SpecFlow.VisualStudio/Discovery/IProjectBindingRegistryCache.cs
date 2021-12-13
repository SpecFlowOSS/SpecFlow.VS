namespace SpecFlow.VisualStudio.Discovery;

public interface IProjectBindingRegistryCache
{
    event EventHandler<EventArgs> Changed;

    Task Update(Func<ProjectBindingRegistry, ProjectBindingRegistry> updateFunc);

    ProjectBindingRegistry Value { get; }
    bool Processing { get; }
    Task<ProjectBindingRegistry> GetLatest();
}
