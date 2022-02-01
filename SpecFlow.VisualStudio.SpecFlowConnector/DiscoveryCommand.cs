namespace SpecFlow.VisualStudio.SpecFlowConnector;

public class DiscoveryCommand : ICommand
{
    public const string CommandName = "discovery";
    private readonly ILogger _log;

    public DiscoveryCommand(ConnectorOptions options, ILogger log)
    {
        _log = log;
    }

    public CommandResult Execute() => new ("{}");
}
