using System;
using System.Collections.Generic;
using System.Reflection;
using TechTalk.SpecFlow;
using TechTalk.SpecFlow.Bindings;
using TechTalk.SpecFlow.Configuration;
using TechTalk.SpecFlow.Infrastructure;

namespace Deveroom.VisualStudio.SpecFlowConnector.Discovery.V21
{
    public class SpecFlowV21Discoverer : RemotingBaseDiscoverer
    {
        protected override IBindingRegistry GetBindingRegistry(Assembly testAssembly, string configFilePath)
        {
            var globalContainer =
                new ContainerBuilder(new NoInvokeDependencyProvider()).CreateGlobalContainer(
                    GetConfigurationProvider());
            var testRunnerManager = (TestRunnerManager)globalContainer.Resolve<ITestRunnerManager>();
            testRunnerManager.Initialize(testAssembly);
            testRunnerManager.CreateTestRunner(0);

            return globalContainer.Resolve<IBindingRegistry>();
        }

        private static DefaultRuntimeConfigurationProvider GetConfigurationProvider()
        {
            return Activator.CreateInstance<DefaultRuntimeConfigurationProvider>();
            //return new DefaultRuntimeConfigurationProvider(configurationLoader);
        }

        protected override IEnumerable<IStepDefinitionBinding> GetStepDefinitions(IBindingRegistry bindingRegistry)
        {
            return bindingRegistry.GetStepDefinitions();
        }
    }
}
