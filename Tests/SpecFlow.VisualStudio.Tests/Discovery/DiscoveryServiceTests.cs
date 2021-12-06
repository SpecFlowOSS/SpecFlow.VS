using SpecFlow.VisualStudio.Annotations;
using SpecFlow.VisualStudio.ProjectSystem.Settings;

namespace SpecFlow.VisualStudio.Tests.Discovery;

public class DiscoveryServiceTests
{
    [Fact]
    public async Task ddd()
    {
        //arrange
        var projectScope = new Mock<IProjectScope>(MockBehavior.Strict);
        var ideScope = new Mock<IIdeScope>(MockBehavior.Strict);
        var logger = new StubLogger();
        var propertyCollection = new PropertyCollection();
        var projectSettingsProvider = new Mock<IProjectSettingsProvider>(MockBehavior.Strict);
        var discoveryResultProvider = new Mock<IDiscoveryResultProvider>(MockBehavior.Strict);
        var projectSettings = new ProjectSettings(
            DeveroomProjectKind.SpecFlowTestProject,
            default, 
            TargetFrameworkMoniker.CreateFromShortName(string.Empty), 
            default, 
            default, 
            default, 
            default, 
            default, 
            string.Empty, 
            default, 
            string.Empty
        );

        projectSettingsProvider.Setup(p => p.GetProjectSettings()).Returns(projectSettings);

        propertyCollection.AddProperty(typeof(IProjectSettingsProvider), projectSettingsProvider.Object);

        ideScope.SetupGet(s => s.Logger).Returns(logger);
        ideScope.SetupGet(s => s.FileSystem).Returns(new MockFileSystem());
        ideScope.SetupGet(s => s.MonitoringService).Returns(
            new MonitoringService(
                new StubAnalyticsTransmitter(logger),
                Mock.Of<IWelcomeService>(),
                Mock.Of<ITelemetryConfigurationHolder>()
            ));
        ideScope.SetupGet(s => s.DeveroomErrorListServices).Returns(Mock.Of<IDeveroomErrorListServices>);

        projectScope.SetupGet(p => p.IdeScope).Returns(ideScope.Object);
        projectScope.SetupGet(p => p.Properties).Returns(propertyCollection);
        projectScope.SetupGet(p => p.ProjectName).Returns(string.Empty);

        discoveryResultProvider
            .Setup(p => p.RunDiscovery(string.Empty, string.Empty, projectSettings))
            .Returns(new DiscoveryResult{StepDefinitions = Array.Empty<StepDefinition>()});

        var discoveryService = new DiscoveryService(projectScope.Object, discoveryResultProvider.Object);

        //act
        discoveryService.InitializeBindingRegistry();
        discoveryService.CheckBindingRegistry();

        //assert
        var registry = await discoveryService.GetBindingRegistryAsync();
        registry.Version.Should().Be(2);
    }
}
