using System;
using System.Collections.Generic;
using System.Reflection;
using TechTalk.SpecFlow;
using TechTalk.SpecFlow.Bindings;
using TechTalk.SpecFlow.Configuration;
using TechTalk.SpecFlow.Infrastructure;

namespace Deveroom.VisualStudio.SpecFlowConnector.Discovery.V30
{
    public class SpecFlowV30P220Discoverer : RemotingBaseDiscoverer
    {
        protected override IBindingRegistry GetBindingRegistry(Assembly testAssembly, string configFilePath)
        {
            // We need to call the BoDi (IObjectContainer) related invocations through reflection, because
            // the real call would try to load IObjectContainer from the TechTalk.SpecFlow assembly.

            IConfigurationLoader configurationLoader = new SpecFlow21ConfigurationLoader(configFilePath);
            var globalContainer = CreateGlobalContainer(configurationLoader, testAssembly);
            ReflectionExtensions.RegisterTypeAs<NoInvokeDependencyProvider.NullBindingInvoker, IBindingInvoker>(globalContainer);

            //var testRunnerManager = (TestRunnerManager)globalContainer.Resolve<ITestRunnerManager>();
            var testRunnerManager = (TestRunnerManager)ReflectionExtensions.Resolve<ITestRunnerManager>(globalContainer);

            InitializeTestRunnerManager(testAssembly, testRunnerManager);

            //return globalContainer.Resolve<IBindingRegistry>();
            return ReflectionExtensions.Resolve<IBindingRegistry>(globalContainer);
        }

        protected virtual object CreateGlobalContainer(IConfigurationLoader configurationLoader, Assembly testAssembly)
        {
            var containerBuilder = new ContainerBuilder(new NoInvokeDependencyProvider());
            var configurationProvider = new DefaultRuntimeConfigurationProvider(configurationLoader);

            //var globalContainer = containerBuilder.CreateGlobalContainer(
            //        testAssembly,
            //        new DefaultRuntimeConfigurationProvider(configurationLoader));
            var globalContainer = containerBuilder.ReflectionCallMethod<object>(nameof(ContainerBuilder.CreateGlobalContainer),
                testAssembly, configurationProvider);
            return globalContainer;
        }

        private void InitializeTestRunnerManager(Assembly testAssembly, TestRunnerManager testRunnerManager)
        {
            testRunnerManager.Initialize(testAssembly);
            testRunnerManager.CreateTestRunner(0);
        }

        protected override IEnumerable<IStepDefinitionBinding> GetStepDefinitions(IBindingRegistry bindingRegistry)
        {
            return bindingRegistry.GetStepDefinitions();
        }
    }
}
