namespace SpecFlow.VisualStudio.Discovery;

public interface IProjectBindingRegistryCache
{
    event EventHandler<EventArgs> Changed;

    Task Update(Func<ProjectBindingRegistry, ProjectBindingRegistry> updateFunc);
    Task Update(Func<ProjectBindingRegistry, Task<ProjectBindingRegistry>> updateTask);

    ProjectBindingRegistry Value { get; }
    bool Processing { get; }
    Task<ProjectBindingRegistry> GetLatest();
}
