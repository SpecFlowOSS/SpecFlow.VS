namespace SpecFlow.VisualStudio.SpecFlowConnector;

public class DiscoveryCommand : ICommand
{
    public const string CommandName = "discovery";

    public DiscoveryCommand(ConnectorOptions options)
    {
    }

    public CommandResult Execute() => new(0, "{}");
}
