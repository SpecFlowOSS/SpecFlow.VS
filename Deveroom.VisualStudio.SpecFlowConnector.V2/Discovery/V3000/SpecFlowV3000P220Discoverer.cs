using System;
using System.Collections.Generic;
using System.Reflection;
using BoDi;
using TechTalk.SpecFlow;
using TechTalk.SpecFlow.Bindings;
using TechTalk.SpecFlow.Configuration;
using TechTalk.SpecFlow.Infrastructure;

namespace Deveroom.VisualStudio.SpecFlowConnector.Discovery.V3000
{
    public class SpecFlowV3000P220Discoverer : BaseDiscoverer
    {
        protected override IBindingRegistry GetBindingRegistry(Assembly testAssembly, string configFilePath)
        {
            IConfigurationLoader configurationLoader = new SpecFlow21ConfigurationLoader(configFilePath);
            var globalContainer = CreateGlobalContainer(configurationLoader, testAssembly);
            var testRunnerManager = (TestRunnerManager)globalContainer.Resolve<ITestRunnerManager>();
            testRunnerManager.Initialize(testAssembly);
            testRunnerManager.CreateTestRunner(0);

            return globalContainer.Resolve<IBindingRegistry>();
        }

        protected virtual IObjectContainer CreateGlobalContainer(IConfigurationLoader configurationLoader, Assembly testAssembly)
        {
            var globalContainer =
                new ContainerBuilder(new NoInvokeDependencyProvider()).CreateGlobalContainer(
                    testAssembly,
                    new DefaultRuntimeConfigurationProvider(configurationLoader));
            return globalContainer;
        }

        protected override IEnumerable<IStepDefinitionBinding> GetStepDefinitions(IBindingRegistry bindingRegistry)
        {
            return bindingRegistry.GetStepDefinitions();
        }
    }
}
