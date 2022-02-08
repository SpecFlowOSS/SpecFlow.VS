using BoDi;
using SpecFlowConnector.Discovery;
using TechTalk.SpecFlow.Plugins;

namespace SpecFlowConnector.SpecFlowProxies;

public class SpecFlowDependencyProviderBeforeV3922 : SpecFlowDependencyProviderVLatest
{
    public SpecFlowDependencyProviderBeforeV3922(AssemblyLoadContext loadContext) : base(loadContext)
    {
    }

    protected override void RegisterRuntimePluginLoader(ObjectContainer globalContainer)
    {
        var pluginLoaderType = new DynamicRuntimePluginLoaderFactory().Create();
        globalContainer.ReflectionRegisterTypeAs(pluginLoaderType, typeof(IRuntimePluginLoader));
    }
}
