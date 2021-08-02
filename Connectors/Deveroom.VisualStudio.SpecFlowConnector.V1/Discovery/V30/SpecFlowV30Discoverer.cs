using System;
using System.Reflection;
using TechTalk.SpecFlow.Configuration;
using TechTalk.SpecFlow.Infrastructure;

namespace Deveroom.VisualStudio.SpecFlowConnector.Discovery.V30
{
    public class SpecFlowV30Discoverer : SpecFlowV30P220Discoverer
    {
        protected override object CreateGlobalContainer(IConfigurationLoader configurationLoader, Assembly testAssembly)
        {
            // We need to call the CreateGlobalContainer differently (no testAssembly passed in) because the interface has been changed in V3.0.220.

            var containerBuilder = new ContainerBuilder(new NoInvokeDependencyProvider());
            var configurationProvider = new DefaultRuntimeConfigurationProvider(configurationLoader);

            //var globalContainer = containerBuilder.CreateGlobalContainer(
            //        new DefaultRuntimeConfigurationProvider(configurationLoader));
            var globalContainer = containerBuilder.ReflectionCallMethod<object>(nameof(ContainerBuilder.CreateGlobalContainer),
                configurationProvider);
            return globalContainer;
        }

    }
}
