using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Loader;
using BoDi;
using TechTalk.SpecFlow;
using TechTalk.SpecFlow.Bindings;
using TechTalk.SpecFlow.Configuration;
using TechTalk.SpecFlow.Infrastructure;

namespace Deveroom.VisualStudio.SpecFlowConnector.Discovery
{
    public class SpecFlowV3BaseDiscoverer : BaseDiscoverer
    {
        protected readonly AssemblyLoadContext _loadContext;

        public SpecFlowV3BaseDiscoverer(AssemblyLoadContext loadContext)
        {
            _loadContext = loadContext;
        }

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
                new ContainerBuilder(CreateDefaultDependencyProvider()).CreateGlobalContainer(
                    testAssembly,
                    new DefaultRuntimeConfigurationProvider(configurationLoader));
            return globalContainer;
        }

        protected virtual DefaultDependencyProvider CreateDefaultDependencyProvider()
        {
            return new NoInvokeDependencyProvider();
        }

        protected override IEnumerable<IStepDefinitionBinding> GetStepDefinitions(IBindingRegistry bindingRegistry)
        {
            return bindingRegistry.GetStepDefinitions();
        }
    }
}
