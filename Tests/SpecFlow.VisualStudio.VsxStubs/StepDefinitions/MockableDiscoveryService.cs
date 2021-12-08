namespace SpecFlow.VisualStudio.VsxStubs.StepDefinitions;

public class MockableDiscoveryService : DiscoveryService
{
    public MockableDiscoveryService(IProjectScope projectScope,
        Mock<IDiscoveryResultProvider> discoveryResultProviderMock)
        : base(projectScope, discoveryResultProviderMock.Object)
    {
    }

    public DiscoveryResult LastDiscoveryResult { get; set; } = new() {StepDefinitions = Array.Empty<StepDefinition>()};

    protected override ConfigSource GetTestAssemblySource(ProjectSettings projectSettings)
    {
        return new ConfigSource("MyAssembly.dll", DateTimeOffset.Parse("2020.12.07")); // fake a valid existing test assembly
    }

    public static MockableDiscoveryService Setup(IProjectScope projectScope, TimeSpan discoveryDelay)
    {
        return SetupWithInitialStepDefinitions(projectScope, Array.Empty<StepDefinition>(), discoveryDelay);
    }

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
            .Returns(()=>
                {
                    Thread.Sleep(discoveryDelay); //make it a bit more realistic
                    return discoveryService.LastDiscoveryResult;
                });

        var initialized = new ManualResetEvent(false);
        discoveryService.BindingRegistryChanged += (_, _) => initialized.Set(); 
        discoveryService.InitializeBindingRegistry();
        projectScope.Properties.AddProperty(typeof(IDiscoveryService), discoveryService);
        initialized.WaitOne(TimeSpan.FromSeconds(10));

        return discoveryService;
    }
}
