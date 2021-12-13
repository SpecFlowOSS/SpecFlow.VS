﻿#nullable enable
namespace SpecFlow.VisualStudio.Discovery;

public interface IDiscoveryService : IDisposable
{
    IProjectBindingRegistryCache BindingRegistryCache { get; }
    event EventHandler<EventArgs> WeakBindingRegistryChanged;
    void CheckBindingRegistry();
    void InitializeBindingRegistry();
}
