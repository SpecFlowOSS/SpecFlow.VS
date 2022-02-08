using TechTalk.SpecFlow.Plugins;

namespace SpecFlowConnector.SpecFlowProxies;

public abstract class LoadContextPluginLoaderBeforeV3922 : RuntimePluginLoaderPatch
{
    private readonly AssemblyLoadContext _loadContext;

    protected LoadContextPluginLoaderBeforeV3922(AssemblyLoadContext loadContext)
    {
        _loadContext = loadContext;
    }

    protected override Assembly LoadAssembly(string pluginAssemblyName) => _loadContext.LoadFromAssemblyPath(pluginAssemblyName);
}
