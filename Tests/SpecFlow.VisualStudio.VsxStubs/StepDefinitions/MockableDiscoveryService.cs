#nullable enable

namespace SpecFlow.VisualStudio.VsxStubs.StepDefinitions;

public class MockableDiscoveryService : DiscoveryService
{
    public MockableDiscoveryService(IProjectScope projectScope,
        Mock<IDiscoveryResultProvider> discoveryResultProviderMock)
        : base(projectScope, discoveryResultProviderMock.Object, new ProjectBindingRegistryCache(projectScope.IdeScope))
    {
    }

    public DiscoveryResult LastDiscoveryResult { get; set; } = new() {StepDefinitions = Array.Empty<StepDefinition>()};

    protected override ConfigSource GetTestAssemblySource(ProjectSettings projectSettings) =>
        new ConfigSource("MyAssembly.dll", DateTimeOffset.Parse("2020.12.07")); // fake a valid existing test assembly

    public static MockableDiscoveryService Setup(IProjectScope projectScope, TimeSpan discoveryDelay) =>
        SetupWithInitialStepDefinitions(projectScope, Array.Empty<StepDefinition>(), discoveryDelay);

    public static MockableDiscoveryService SetupWithInitialStepDefinitions(IProjectScope projectScope,
        StepDefinition[] stepDefinitions, TimeSpan discoveryDelay)
    {
        var discoveryResultProviderMock = new Mock<IDiscoveryResultProvider>(MockBehavior.Strict);

        var discoveryService = new MockableDiscoveryService(projectScope, discoveryResultProviderMock)
        {
            LastDiscoveryResult = new DiscoveryResult {StepDefinitions = stepDefinitions}
        };

        discoveryResultProviderMock
            .Setup(ds => ds.RunDiscovery(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ProjectSettings>()))
            .Returns(() =>
            {
                Thread.Sleep(discoveryDelay); //make it a bit more realistic
                return discoveryService.LastDiscoveryResult;
            });

        Initialize(discoveryService, projectScope.GetProjectSettings());

        projectScope.Properties.AddProperty(typeof(IDiscoveryService), discoveryService);
        return discoveryService;
    }

    private static void Initialize(MockableDiscoveryService discoveryService, ProjectSettings projectSettings)
    {
        if (!projectSettings.IsSpecFlowTestProject) return;

        var initialized = new ManualResetEvent(false);
        discoveryService.BindingRegistryCache.Changed += (_, _) => initialized.Set();
        discoveryService.InitializeBindingRegistry();
        initialized.WaitOne(TimeSpan.FromSeconds(5)).Should().BeTrue("initialization have to be done quickly in a mock");
    }
}
