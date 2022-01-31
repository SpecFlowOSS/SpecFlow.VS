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
        switch (options.CommandName)
        {
            case DiscoveryCommand.CommandName: return new DiscoveryCommand(options);
            //case GeneratorCommand.CommandName: return new GeneratorCommand(options);
            default: return new ArgumentException($"Invalid command: {options.CommandName}");
        }
    }
}
