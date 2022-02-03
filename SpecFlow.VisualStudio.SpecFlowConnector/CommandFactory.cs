using SpecFlowConnector.Discovery;

namespace SpecFlow.VisualStudio.SpecFlowConnector;

public class CommandFactory
{
    private readonly ILogger _log;
    private readonly IFileSystem _fileSystem;

    public CommandFactory(ILogger log, IFileSystem fileSystem)
    {
        _log = log;
        _fileSystem = fileSystem;
    }

    public Either<Exception, ICommand> CreateCommand(string[] args) =>
        args
            .Map(ConnectorOptions.Parse)
            .Tie(DumpOptions)
            .Tie(AttachDebuggerWhenRequired)
            .Map(ToCommand);

    public void DumpOptions(ConnectorOptions options) => _log.Debug(options.ToString());

    public static void AttachDebuggerWhenRequired(ConnectorOptions connectorOptions)
    {
        if (connectorOptions.DebugMode && !Debugger.IsAttached)
            Debugger.Launch();
    }

    public Either<Exception, ICommand> ToCommand(ConnectorOptions options)
    {
        return options switch
        {
            DiscoveryOptions o => new DiscoveryCommand(o, _log, _fileSystem),
            _ => new ArgumentException($"Invalid command: {options.GetType().Name}")
        };
    }
}
