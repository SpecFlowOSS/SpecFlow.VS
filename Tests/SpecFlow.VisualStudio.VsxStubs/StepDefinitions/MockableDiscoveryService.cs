using System;
using System.Threading;
using Moq;
using SpecFlow.VisualStudio.Discovery;
using SpecFlow.VisualStudio.ProjectSystem;
using SpecFlow.VisualStudio.ProjectSystem.Configuration;
using SpecFlow.VisualStudio.ProjectSystem.Settings;
using SpecFlow.VisualStudio.SpecFlowConnector.Models;

namespace SpecFlow.VisualStudio.VsxStubs.StepDefinitions
{
    public class MockableDiscoveryService : DiscoveryService
    {
        public DiscoveryResult LastDiscoveryResult { get; set; } = new DiscoveryResult() { StepDefinitions = new StepDefinition[0]};
        public DateTime LastVersion { get; set; } = DateTime.UtcNow;
        public bool IsDiscoveryPerformed { get; set; }

        public MockableDiscoveryService(IProjectScope projectScope, Mock<IDiscoveryResultProvider> discoveryResultProviderMock) : base(projectScope, discoveryResultProviderMock.Object)
        {
            discoveryResultProviderMock.Setup(ds => ds.RunDiscovery(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ProjectSettings>())).Returns(
                delegate
                {
                    System.Threading.Thread.Sleep(100); //make it a bit more realistic
                    return LastDiscoveryResult;
                });
        }

        protected override void TriggerBindingRegistryChanged()
        {
            base.TriggerBindingRegistryChanged();
            if (GetBindingRegistry() != null)
                IsDiscoveryPerformed = true;
        }

        protected override ConfigSource GetTestAssemblySource(ProjectSettings projectSettings)
        {
            return new ConfigSource("MyAssembly.dll", LastVersion); // fake a valid existing test assembly
        }

        public static MockableDiscoveryService Setup(IProjectScope projectScope)
        {
            return SetupWithInitialStepDefinitions(projectScope, Array.Empty<StepDefinition>());
        }

        public static MockableDiscoveryService SetupWithInitialStepDefinitions(IProjectScope projectScope, StepDefinition[] stepDefinitions)
        {
            var initialDiscoveryResult = new DiscoveryResult() { StepDefinitions = stepDefinitions };
            var discoveryResultProviderMock = new Mock<IDiscoveryResultProvider>(MockBehavior.Strict);
            discoveryResultProviderMock
                .Setup(ds => ds.RunDiscovery(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ProjectSettings>()))
                .Returns(() => initialDiscoveryResult);

            var discoveryService = new MockableDiscoveryService(projectScope, discoveryResultProviderMock)
            {
                LastDiscoveryResult = initialDiscoveryResult
            };

            projectScope.Properties.AddProperty(typeof(IDiscoveryService), discoveryService);
            return discoveryService;
        }

        public void WaitUntilDiscoveryPerformed()
        {
            while (!IsDiscoveryPerformed)
            {
                Thread.Sleep(10);
            }
        }
    }
}