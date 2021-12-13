namespace SpecFlow.VisualStudio.Discovery;

public interface IDiscoveryService : IDisposable
{
    event EventHandler<EventArgs> WeakBindingRegistryChanged;
    void CheckBindingRegistry();
    void InitializeBindingRegistry();

    IProjectBindingRegistryCache BindingRegistryCache { get; }
}
