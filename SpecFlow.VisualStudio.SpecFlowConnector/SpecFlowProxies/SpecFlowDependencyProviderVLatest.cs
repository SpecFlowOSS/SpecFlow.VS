using BoDi;
using SpecFlowConnector.Discovery;
using TechTalk.SpecFlow.Plugins;

namespace SpecFlowConnector.SpecFlowProxies;

public class SpecFlowDependencyProviderVLatest : NoInvokeDependencyProvider
{
    protected readonly AssemblyLoadContext _loadContext;

    public SpecFlowDependencyProviderVLatest(AssemblyLoadContext loadContext)
    {
        _loadContext = loadContext;
    }

    public override void RegisterGlobalContainerDefaults(ObjectContainer globalContainer)
    {
        base.RegisterGlobalContainerDefaults(globalContainer);
        globalContainer.RegisterInstanceAs(_loadContext);
        RegisterRuntimePluginLoader(globalContainer);
    }

    protected virtual void RegisterRuntimePluginLoader(ObjectContainer globalContainer)
    {
        globalContainer.RegisterTypeAs<LoadContextPluginLoaderVLatest, IRuntimePluginLoader>();
    }
}
