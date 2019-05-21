using System;
using System.Threading.Tasks;

namespace Deveroom.VisualStudio.Discovery
{
    public interface IDiscoveryService : IDisposable
    {
        event EventHandler<EventArgs> WeakBindingRegistryChanged;
        ProjectBindingRegistry GetBindingRegistry();
        Task<ProjectBindingRegistry> GetBindingRegistryAsync();
        void CheckBindingRegistry();
    }
}