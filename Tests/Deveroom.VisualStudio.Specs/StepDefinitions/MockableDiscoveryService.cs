using System;
using Deveroom.VisualStudio.Discovery;
using Deveroom.VisualStudio.ProjectSystem;
using Deveroom.VisualStudio.ProjectSystem.Configuration;
using Deveroom.VisualStudio.ProjectSystem.Settings;
using Deveroom.VisualStudio.SpecFlowConnector.Models;
using Moq;

namespace Deveroom.VisualStudio.Specs.StepDefinitions
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
            var discoveryResultProviderMock = new Mock<IDiscoveryResultProvider>();
            var discoveryService = new MockableDiscoveryService(projectScope, discoveryResultProviderMock);
            projectScope.Properties.AddProperty(typeof(IDiscoveryService), discoveryService);
            return discoveryService;
        }
    }
}