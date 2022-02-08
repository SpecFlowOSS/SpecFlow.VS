using TechTalk.SpecFlow.Plugins;
using TechTalk.SpecFlow.Tracing;

namespace SpecFlowConnector.SpecFlowProxies;

public interface IRuntimePluginLoaderBeforeV3922 : IRuntimePluginLoader
{
    IRuntimePlugin LoadPlugin(string pluginAssemblyName, ITraceListener traceListener);
}
