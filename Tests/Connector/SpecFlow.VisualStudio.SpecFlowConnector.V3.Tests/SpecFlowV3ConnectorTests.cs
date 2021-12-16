using System;
using FluentAssertions;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using SpecFlow.VisualStudio.SpecFlowConnector.Discovery;
using SpecFlow.VisualStudio.SpecFlowConnector.Models;
using TechTalk.SpecFlow;
using Xunit;

namespace SpecFlow.VisualStudio.SpecFlowConnector.V3.Tests;
// This test class is primarily for debugging connectors. The full compatibility matrix is verified by the
// scenarios in DiscoverySpecFlowVersionCompatibility.feature. 
// In order to debug a particular SpecFlow version and test execution framework, set the TestedSpecFlowVersion
// and TestedSpecFlowTestFramework properties in the project file.

public class SpecFlowV3ConnectorTests
{
    private IDiscoveryResultDiscoverer CreateSut()
    {
        var versionSelectorDiscoverer = new VersionSelectorDiscoverer(AssemblyLoadContext.Default);
        versionSelectorDiscoverer.EnsureDiscoverer();
        versionSelectorDiscoverer.Discoverer.Should().BeAssignableTo<IDiscoveryResultDiscoverer>();
        return (IDiscoveryResultDiscoverer) versionSelectorDiscoverer.Discoverer;
    }

    private string GetTestAssemblyPath() => Assembly.GetExecutingAssembly().Location;

    private DiscoveryResult PerformDiscover(IDiscoveryResultDiscoverer sut)
    {
        var testAssemblyPath = GetTestAssemblyPath();
        var testAssembly = Assembly.LoadFrom(testAssemblyPath);
        return sut.DiscoverInternal(testAssembly, testAssemblyPath, null);
    }

    [Fact]
    public void Discovers_step_definitions()
    {
        var sut = CreateSut();

        var result = PerformDiscover(sut);

        result.StepDefinitions.Should().NotBeNullOrEmpty();
    }

    [Fact]
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

    [Binding]
    public class SampleBindings
    {
        public static bool BeforeTestRunHookCalled;

        public static bool AfterTestRunHookCalled;

        [When(@"I press add")]
        public void WhenIPressAdd()
        {
        }

        [BeforeTestRun]
        public static void BeforeTestRunHook()
        {
            BeforeTestRunHookCalled = true;
        }

        [AfterTestRun]
        public static void AfterTestRunHook()
        {
            AfterTestRunHookCalled = true;
        }
    }
}
