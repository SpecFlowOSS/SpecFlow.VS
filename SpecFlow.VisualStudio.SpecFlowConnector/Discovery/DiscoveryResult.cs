using Newtonsoft.Json;

namespace SpecFlowConnector.Discovery;

public class DiscoveryResult
{
    public DiscoveryResult(ImmutableArray<StepDefinition> StepDefinitions,
        ImmutableSortedDictionary<string, string> SourceFiles,
        ImmutableSortedDictionary<string, string> TypeNames,
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        string? ErrorMessage)
    {
        this.StepDefinitions = StepDefinitions;
        this.SourceFiles = SourceFiles;
        this.TypeNames = TypeNames;
        this.ErrorMessage = ErrorMessage;
    }

    public ImmutableArray<StepDefinition> StepDefinitions { get; init; }
    public ImmutableSortedDictionary<string, string> SourceFiles { get; init; }
    public ImmutableSortedDictionary<string, string> TypeNames { get; init; }
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string? ErrorMessage { get; init; }
}
