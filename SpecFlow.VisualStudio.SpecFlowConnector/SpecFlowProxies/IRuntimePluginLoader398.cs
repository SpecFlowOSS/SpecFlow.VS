using TechTalk.SpecFlow.Plugins;
using TechTalk.SpecFlow.Tracing;

namespace SpecFlowConnector.SpecFlowProxies;

public interface IRuntimePluginLoader398 : IRuntimePluginLoader
{
    IRuntimePlugin LoadPlugin(string pluginAssemblyName, ITraceListener traceListener);
}
