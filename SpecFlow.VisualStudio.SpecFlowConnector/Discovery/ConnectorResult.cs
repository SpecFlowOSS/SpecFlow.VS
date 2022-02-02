namespace SpecFlowConnector.Discovery;

public abstract record ConnectorResult(string SpecFlowVersion, string ErrorMessage, ImmutableArray<string> Warnings)
{
    public bool IsFailed => !string.IsNullOrWhiteSpace(ErrorMessage);
}
