using System;
using TechTalk.SpecFlow.Configuration;

namespace SpecFlow.VisualStudio.SpecFlowConnector.Discovery
{
    public class SpecFlowV21Discoverer : SpecFlowV22Discoverer
    {
        protected override IRuntimeConfigurationProvider CreateConfigurationProvider(string configFilePath)
        {
            return Activator.CreateInstance<DefaultRuntimeConfigurationProvider>();
        }
    }
}
