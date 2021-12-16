#nullable enable
namespace SpecFlow.VisualStudio.VsxStubs;

public class StubDiscoveryResultProvider : IDiscoveryResultProvider
{
    public DiscoveryResult DiscoveryResult { get; set; } = new() {StepDefinitions = Array.Empty<StepDefinition>()};

    public DiscoveryResult
        RunDiscovery(string testAssemblyPath, string configFilePath, ProjectSettings projectSettings) =>
        DiscoveryResult;
}
