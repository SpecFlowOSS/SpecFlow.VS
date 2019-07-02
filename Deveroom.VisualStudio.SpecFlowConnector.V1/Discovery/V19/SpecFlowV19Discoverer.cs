using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BoDi;
using TechTalk.SpecFlow;
using TechTalk.SpecFlow.Bindings;
using TechTalk.SpecFlow.Configuration;
using TechTalk.SpecFlow.Infrastructure;

namespace Deveroom.VisualStudio.SpecFlowConnector.Discovery.V19
{
    public class SpecFlowV19Discoverer : RemotingBaseDiscoverer
    {
        protected override IBindingRegistry GetBindingRegistry(Assembly testAssembly, string configFilePath)
        {
            var testRunner = CreateTestRunner(testAssembly);
            var testExecutionEngine = testRunner.ReflectionGetField<ITestExecutionEngine>("executionEngine");
            return testExecutionEngine.ReflectionGetField<IBindingRegistry>("bindingRegistry");
        }

        private ITestRunner CreateTestRunner(Assembly testAssembly)
        {
            // IObjectContainer container = TestRunContainerBuilder.CreateContainer((IRuntimeConfigurationProvider)null);
            IObjectContainer container = typeof(ITestRunner).Assembly
                .GetType("TechTalk.SpecFlow.Infrastructure.TestRunContainerBuilder", true)
                .ReflectionCallStaticMethod<IObjectContainer>("CreateContainer",
                    new[] {typeof(IRuntimeConfigurationProvider)}, new object[] {null});
            // reset IBindingInvoker to avoid invoking hooks (Issue #27) 
            container.RegisterTypeAs<NoInvokeDependencyProvider.NullBindingInvoker, IBindingInvoker>();
            return container.Resolve<ITestRunnerFactory>().Create(testAssembly);
        }

        protected override IEnumerable<IStepDefinitionBinding> GetStepDefinitions(IBindingRegistry bindingRegistry)
        {
            return bindingRegistry.GetConsideredStepDefinitions(StepDefinitionType.Given)
                .Concat(bindingRegistry.GetConsideredStepDefinitions(StepDefinitionType.When))
                .Concat(bindingRegistry.GetConsideredStepDefinitions(StepDefinitionType.Then));
        }
    }
}
