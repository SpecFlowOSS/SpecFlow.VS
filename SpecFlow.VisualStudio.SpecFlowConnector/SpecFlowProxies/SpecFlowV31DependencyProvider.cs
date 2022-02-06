using BoDi;
using SpecFlow.VisualStudio.SpecFlowConnector.Discovery;
using TechTalk.SpecFlow.Plugins;

namespace SpecFlowConnector.SpecFlowProxies;

public class SpecFlowV31DependencyProvider : NoInvokeDependencyProvider
{
    private readonly AssemblyLoadContext _loadContext; //TODO: szerintem ez nem kell

    public SpecFlowV31DependencyProvider(AssemblyLoadContext loadContext)
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
