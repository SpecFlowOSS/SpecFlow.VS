namespace SpecFlowConnector.Discovery;

public record DiscoveryResult(
        ImmutableHashSet<StepDefinition> StepDefinitions,
        //ImmutableDictionary<string, string> SourceFiles,
        //ImmutableDictionary<string, string> TypeNames,
        string SpecFlowVersion,
        string ErrorMessage,
        ImmutableArray<string> Warnings
        )
    : ConnectorResult(
        SpecFlowVersion,
        ErrorMessage,
        Warnings)
{
}