using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BoDi;
using TechTalk.SpecFlow;
using TechTalk.SpecFlow.Bindings;
using TechTalk.SpecFlow.Configuration;
using TechTalk.SpecFlow.Infrastructure;

namespace Deveroom.VisualStudio.SpecFlowConnector.Discovery.V20
{
    public class SpecFlowV20Discoverer : RemotingBaseDiscoverer
    {
        protected override IBindingRegistry GetBindingRegistry(Assembly testAssembly, string configFilePath)
        {
            var testRunner = CreateTestRunner(testAssembly);
            var testExecutionEngine = testRunner.ReflectionGetField<ITestExecutionEngine>("executionEngine");
            return testExecutionEngine.ReflectionGetField<IBindingRegistry>("bindingRegistry");
        }

        private static ITestRunner CreateTestRunner(Assembly testAssembly)
        {
            var testRunContainerBuilderType = typeof(ITestRunner).Assembly
                .GetType("TechTalk.SpecFlow.Infrastructure.TestRunContainerBuilder", true);
            var testRunContainerBuilder =
                testRunContainerBuilderType.ReflectionCreateInstance<object>(
                    new[] { typeof(IDefaultDependencyProvider) }, new object[] {null});
            IObjectContainer container = testRunContainerBuilder.ReflectionCallMethod<IObjectContainer>(
                "CreateContainer", new[] {typeof(IRuntimeConfigurationProvider)}, new object[] {null});

            // reset IBindingInvoker to avoid invoking hooks (Issue #27) 
            container.RegisterTypeAs<NoInvokeDependencyProvider.NullBindingInvoker, IBindingInvoker>();

            var testRunnerManager = container.Resolve<ITestRunnerManager>();
            testRunnerManager.Initialize(testAssembly);
            return testRunnerManager.GetTestRunner(0);
        }

        protected override IEnumerable<IStepDefinitionBinding> GetStepDefinitions(IBindingRegistry bindingRegistry)
        {
            return bindingRegistry.GetConsideredStepDefinitions(StepDefinitionType.Given)
                .Concat(bindingRegistry.GetConsideredStepDefinitions(StepDefinitionType.When))
                .Concat(bindingRegistry.GetConsideredStepDefinitions(StepDefinitionType.Then));
        }
    }
}
