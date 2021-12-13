#nullable enable
namespace SpecFlow.VisualStudio.Discovery;

public interface IProjectBindingRegistryCache
{
    ProjectBindingRegistry Value { get; }
    bool Processing { get; }
    event EventHandler<EventArgs> Changed;

    Task Update(Func<ProjectBindingRegistry, ProjectBindingRegistry> updateFunc);
    Task<ProjectBindingRegistry> GetLatest();
}
