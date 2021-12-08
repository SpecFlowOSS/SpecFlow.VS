﻿namespace SpecFlow.VisualStudio.Discovery;

public interface IDiscoveryService : IDisposable
{
    event EventHandler<EventArgs> WeakBindingRegistryChanged;
    ProjectBindingRegistry GetLastProcessedBindingRegistry();
    Task<ProjectBindingRegistry> GetLatestBindingRegistry();
    void CheckBindingRegistry();
    void InitializeBindingRegistry();
    ManualResetEvent Initialized { get; }

    Task UpdateBindingRegistry(Func<ProjectBindingRegistry, ProjectBindingRegistry> update);
    Task ProcessAsync(CSharpStepDefinitionFile stepDefinitionFile);
}
