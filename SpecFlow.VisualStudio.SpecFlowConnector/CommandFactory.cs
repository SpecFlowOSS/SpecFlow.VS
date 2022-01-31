namespace SpecFlow.VisualStudio.SpecFlowConnector;

public class CommandFactory
{
    public static Either<Exception, ICommand> CreateCommand(string[] args) =>
        args
            .Map(ConnectorOptions.Parse)
            .Tie(AttachDebuggerWhenRequired)
            .Map(ToCommand);

    public static void AttachDebuggerWhenRequired(ConnectorOptions connectorOptions)
    {
        if (connectorOptions.DebugMode && !Debugger.IsAttached)
            Debugger.Launch();
    }

    public static Either<Exception, ICommand> ToCommand(ConnectorOptions options)
    {
        return options switch
        {
            DiscoveryOptions o => new DiscoveryCommand(o),
            _ => new ArgumentException($"Invalid command: {options.GetType().Name}")
        };
    }
}
