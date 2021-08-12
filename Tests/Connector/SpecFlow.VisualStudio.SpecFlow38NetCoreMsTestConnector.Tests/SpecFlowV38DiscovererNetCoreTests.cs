using System;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SpecFlow.VisualStudio.SpecFlowConnector.Discovery.V31;
using SpecFlow.VisualStudio.SpecFlowConnector.Discovery.V38;
using SpecFlow.VisualStudio.SpecFlowConnector.Models;
using TechTalk.SpecFlow;

namespace SpecFlow.VisualStudio.SpecFlow38NetCoreMsTestConnector.Tests
{
    [TestClass]
    public class SpecFlowV38MsTestDiscovererNetCoreTests
    {
        private SpecFlowV38Discoverer CreateSut()
        {
            var stubDiscoverer = new SpecFlowV38Discoverer(AssemblyLoadContext.Default);
            return stubDiscoverer;
        }

        private string GetTestAssemblyPath()
        {
            return Assembly.GetExecutingAssembly().Location;
        }

        private DiscoveryResult PerformDiscover(SpecFlowV38Discoverer sut)
        {
            var testAssemblyPath = GetTestAssemblyPath();
            var testAssembly = Assembly.LoadFrom(testAssemblyPath);
            return sut.DiscoverInternal(testAssembly, testAssemblyPath, null);
        }

        [Binding]
        public class SampleBindings
        {
            [When(@"I press add")]
            public void WhenIPressAdd() { }

            public static bool BeforeTestRunHookCalled = false;
            [BeforeTestRun]
            public static void BeforeTestRunHook()
            {
                BeforeTestRunHookCalled = true;
            }

            public static bool AfterTestRunHookCalled = false;
            [AfterTestRun]
            public static void AfterTestRunHook()
            {
                AfterTestRunHookCalled = true;
            }
        }

        [TestMethod]
        public void Discovers_step_definitions()
        {
            var sut = CreateSut();

            var result = PerformDiscover(sut);

            result.StepDefinitions.Should().NotBeNullOrEmpty();
        }

        [TestMethod]
        public void Should_not_invoke_BeforeAfterTestRun_hook_during_discovery_Issue_27()
        {
            var sut = CreateSut();
            SampleBindings.BeforeTestRunHookCalled = false;
            SampleBindings.AfterTestRunHookCalled = false;

            var result = PerformDiscover(sut);

            result.StepDefinitions.Should().NotBeNullOrEmpty();
            SampleBindings.AfterTestRunHookCalled.Should().BeFalse();
            SampleBindings.BeforeTestRunHookCalled.Should().BeFalse();
        }
    }
}
