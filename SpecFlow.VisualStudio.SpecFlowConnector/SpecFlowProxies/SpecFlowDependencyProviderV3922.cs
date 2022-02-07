using BoDi;
using SpecFlow.VisualStudio.SpecFlowConnector.Discovery;
using TechTalk.SpecFlow.Plugins;

namespace SpecFlowConnector.SpecFlowProxies;

public class SpecFlowDependencyProviderV398 : SpecFlowDependencyProviderV3922
{
    public SpecFlowDependencyProviderV398(AssemblyLoadContext loadContext) : base(loadContext)
    {
    }
}

public class SpecFlowDependencyProviderV3922 : NoInvokeDependencyProvider
{
    private readonly AssemblyLoadContext _loadContext;

    public SpecFlowDependencyProviderV3922(AssemblyLoadContext loadContext)
    {
        _loadContext = loadContext;
    }

    public override void RegisterGlobalContainerDefaults(ObjectContainer globalContainer)
    {
        base.RegisterGlobalContainerDefaults(globalContainer);
        globalContainer.RegisterInstanceAs(_loadContext);
        globalContainer.RegisterTypeAs<LoadContextPluginLoader, IRuntimePluginLoader>();
    }
}
