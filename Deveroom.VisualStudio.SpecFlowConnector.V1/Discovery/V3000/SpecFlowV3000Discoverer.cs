using System;
using System.Collections.Generic;
using System.Reflection;
using TechTalk.SpecFlow;
using TechTalk.SpecFlow.Bindings;
using TechTalk.SpecFlow.Configuration;
using TechTalk.SpecFlow.Infrastructure;

namespace Deveroom.VisualStudio.SpecFlowConnector.Discovery.V3000
{
    public class SpecFlowV3000Discoverer : RemotingBaseDiscoverer
    {
        protected override IBindingRegistry GetBindingRegistry(Assembly testAssembly, string configFilePath)
        {
            IConfigurationLoader configurationLoader = new SpecFlow21ConfigurationLoader(configFilePath);
            var containerBuilder = new ContainerBuilder(new NoInvokeDependencyProvider());
            var configurationProvider = new DefaultRuntimeConfigurationProvider(configurationLoader);

            // We need to call the BoDi (IObjectContainer) related invocations through reflection, because
            // the real call would try to load IObjectContainer from the TechTalk.SpecFlow assembly.

            //var globalContainer = containerBuilder.CreateGlobalContainer(
            //        new DefaultRuntimeConfigurationProvider(configurationLoader));
            var globalContainer = containerBuilder.ReflectionCallMethod<object>(nameof(ContainerBuilder.CreateGlobalContainer), configurationProvider);
            RegisterTypeAs<NoInvokeDependencyProvider.NullBindingInvoker, IBindingInvoker>(globalContainer);

            //var testRunnerManager = (TestRunnerManager)globalContainer.Resolve<ITestRunnerManager>();
            var testRunnerManager = (TestRunnerManager)Resolve<ITestRunnerManager>(globalContainer);

            InitializeTestRunnerManager(testAssembly, testRunnerManager);

            //return globalContainer.Resolve<IBindingRegistry>();
            return Resolve<IBindingRegistry>(globalContainer);
        }

        private T Resolve<T>(object container)
        {
            return container.ReflectionCallMethod<T>(nameof(BoDi.IObjectContainer.Resolve),
                new[] { typeof(Type), typeof(string) },
                typeof(T), null);
        }

        private void RegisterTypeAs<TType, TInterface>(object container) where TType : class, TInterface
        {
            container.ReflectionCallMethod(nameof(BoDi.IObjectContainer.RegisterTypeAs),
                new[] { typeof(Type), typeof(Type) },
                typeof(TType), typeof( TInterface));
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
