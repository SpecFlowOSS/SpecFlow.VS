using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
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
        private TaskCompletionSource<bool> _discoveryCompletionSource = new TaskCompletionSource<bool>();
        public DiscoveryResult LastDiscoveryResult { get; set; } = new DiscoveryResult() { StepDefinitions = new StepDefinition[0]};
        public DateTime LastVersion { get; private set; } = DateTime.UtcNow;

        public MockableDiscoveryService(IProjectScope projectScope, Mock<IDiscoveryResultProvider> discoveryResultProviderMock) 
            : base(projectScope, discoveryResultProviderMock.Object)
        {
        }

        public void Invalidate()
        {
            LastVersion = DateTime.UtcNow;
            var dcs = Interlocked.Exchange(ref _discoveryCompletionSource, new TaskCompletionSource<bool>());
            dcs.SetResult(false);
        }

        protected override void TriggerBindingRegistryChanged()
        {
            base.TriggerBindingRegistryChanged();
            if (GetBindingRegistry() is null) return;

            var dcs = Interlocked.Exchange(ref _discoveryCompletionSource, new TaskCompletionSource<bool>());
            dcs.SetResult(true);
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

        public Task WaitUntilDiscoveryPerformed()
        {
            CancellationTokenSource cts = Debugger.IsAttached
                ? new CancellationTokenSource(TimeSpan.FromMinutes(1))
                : new CancellationTokenSource(TimeSpan.FromSeconds(5));
            return WaitUntilDiscoveryPerformed(cts.Token);
        }

        public async Task WaitUntilDiscoveryPerformed(CancellationToken timeOutToken)
        {
            var timeout = Task.Delay(-1, timeOutToken);
            Task<bool> result;
            do
            {
                result = await Task.WhenAny(_discoveryCompletionSource.Task, timeout) as Task<bool>;
                if (timeOutToken.IsCancellationRequested) throw new TaskCanceledException("Discovery is not performed in time");
            } while (result?.Result != true);
        }
    }
}
