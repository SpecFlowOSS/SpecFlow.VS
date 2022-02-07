using SpecFlowConnector.Discovery;

namespace SpecFlowConnector;

public class CommandFactory
{
    private readonly ILogger _log;
    private readonly IFileSystem _fileSystem;
    private readonly ConnectorOptions _options;
    private readonly Assembly _testAssembly;

    public CommandFactory(ILogger log, IFileSystem fileSystem, ConnectorOptions options, Assembly testAssembly)
    {
        _log = log;
        _fileSystem = fileSystem;
        _options = options;
        _testAssembly = testAssembly;
    }

    public Either<Exception, ICommand> CreateCommand() =>
        _options
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
            DiscoveryOptions o => new DiscoveryCommand(
                o.ConfigFile?.Map(FileDetails.FromPath) ?? None<FileDetails>.Value,
                _log, 
                _fileSystem,
                _testAssembly),
            _ => new ArgumentException($"Invalid command: {options.GetType().Name}")
        };
    }
}
