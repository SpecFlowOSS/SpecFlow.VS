namespace SpecFlow.VisualStudio.SpecFlowConnector;

public class CommandFactory
{
    private readonly ILogger _log;

    public CommandFactory(ILogger log)
    {
        _log = log;
    }

    public Either<Exception, ICommand> CreateCommand(string[] args) =>
        args
            .Map(ConnectorOptions.Parse)
            .Tie(AttachDebuggerWhenRequired)
            .Map(ToCommand);

    public static void AttachDebuggerWhenRequired(ConnectorOptions connectorOptions)
    {
        if (connectorOptions.DebugMode && !Debugger.IsAttached)
            Debugger.Launch();
    }

    public Either<Exception, ICommand> ToCommand(ConnectorOptions options)
    {
        return options switch
        {
            DiscoveryOptions o => new DiscoveryCommand(o, _log),
            _ => new ArgumentException($"Invalid command: {options.GetType().Name}")
        };
    }
}
