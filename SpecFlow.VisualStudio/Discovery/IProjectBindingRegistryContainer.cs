namespace SpecFlow.VisualStudio.Discovery;

public interface IProjectBindingRegistryContainer
{
    event EventHandler<EventArgs> Changed;

    Task Update(Func<ProjectBindingRegistry, ProjectBindingRegistry> updateFunc);

    ProjectBindingRegistry Cache { get; }
    bool Processing { get; }    
    Task<ProjectBindingRegistry> GetLatest();
}
