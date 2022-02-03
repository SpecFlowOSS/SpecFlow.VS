namespace SpecFlowConnector.Discovery;

public record DiscoveryResult(
        ImmutableHashSet<StepDefinition> StepDefinitions,
        //ImmutableDictionary<string, string> SourceFiles,
        //ImmutableDictionary<string, string> TypeNames,
        string ErrorMessage
        );
