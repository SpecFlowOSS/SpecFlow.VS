﻿namespace SpecFlow.VisualStudio.SpecFlowConnector.Discovery;

public interface IRuntimePluginLoader_3_0_220 : IRuntimePluginLoader
{
    IRuntimePlugin LoadPlugin(string pluginAssemblyName, ITraceListener traceListener);
}
