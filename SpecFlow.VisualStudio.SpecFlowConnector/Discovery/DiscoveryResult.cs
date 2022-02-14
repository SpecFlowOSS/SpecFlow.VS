
namespace SpecFlowConnector.Discovery;

public class DiscoveryResult
{
    public DiscoveryResult(ImmutableArray<StepDefinition> stepDefinitions,
        ImmutableSortedDictionary<string, string> sourceFiles,
        ImmutableSortedDictionary<string, string> typeNames,
        ImmutableSortedDictionary<string, string> analyticsProperties,
        string? errorMessage)
    {
        this.StepDefinitions = stepDefinitions;
        this.SourceFiles = sourceFiles;
        this.TypeNames = typeNames;
        AnalyticsProperties = analyticsProperties;
        this.ErrorMessage = errorMessage;
    }

    public ImmutableArray<StepDefinition> StepDefinitions { get; init; }
    public ImmutableSortedDictionary<string, string> SourceFiles { get; init; }
    public ImmutableSortedDictionary<string, string> TypeNames { get; init; }
    public ImmutableSortedDictionary<string, string> AnalyticsProperties { get; init; }
    public string? ErrorMessage { get; init; }
}
