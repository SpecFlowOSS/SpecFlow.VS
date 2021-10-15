using System;
using System.Threading.Tasks;

namespace SpecFlow.VisualStudio.Discovery
{
    public interface IDiscoveryService : IDisposable
    {
        event EventHandler<EventArgs> WeakBindingRegistryChanged;
        ProjectBindingRegistry GetBindingRegistry();
        Task<ProjectBindingRegistry> GetBindingRegistryAsync();
        void CheckBindingRegistry();
        void InitializeBindingRegistry();
        void ReplaceBindingRegistry(ProjectBindingRegistry bindingRegistry);
        Task ProcessAsync(CSharpStepDefinitionFile stepDefinitionFile);
    }
}