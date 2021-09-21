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

        public MockableDiscoveryService(IProjectScope projectScope, Mock<IDiscoveryResultProvider> discoveryResultProviderMock) 
            : base(projectScope, discoveryResultProviderMock.Object)
        {
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

        public static MockableDiscoveryService Setup(IProjectScope projectScope, TimeSpan discoveryDelay)
        {
            return SetupWithInitialStepDefinitions(projectScope, Array.Empty<StepDefinition>(), discoveryDelay);
        }

        public static MockableDiscoveryService SetupWithInitialStepDefinitions(IProjectScope projectScope, StepDefinition[] stepDefinitions, TimeSpan discoveryDelay)
        {
            var discoveryResultProviderMock = new Mock<IDiscoveryResultProvider>(MockBehavior.Strict);

            var discoveryService = new MockableDiscoveryService(projectScope, discoveryResultProviderMock)
            {
                LastDiscoveryResult = new DiscoveryResult() { StepDefinitions = stepDefinitions }
            };

            discoveryResultProviderMock.Setup(ds => ds.RunDiscovery(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ProjectSettings>())).Returns(
                delegate
                {
                    Thread.Sleep(discoveryDelay); //make it a bit more realistic
                    return discoveryService.LastDiscoveryResult;
                });

            discoveryService.InitializeBindingRegistry();

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