namespace SpecFlow.VisualStudio.Discovery;

public interface IProjectBindingRegistryContainer
{
    bool CacheIsUpToDate { get; }
    event EventHandler<EventArgs> Changed;
    Task Update(Func<ProjectBindingRegistry, ProjectBindingRegistry> updateFunc);
    ProjectBindingRegistry Cache { get; }
    Task<ProjectBindingRegistry> GetLatest();
}
