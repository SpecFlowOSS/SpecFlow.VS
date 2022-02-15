namespace SpecFlowConnector;

public class CommandFactory
{
    private readonly IAnalyticsContainer _analytics;
    private readonly IFileSystem _fileSystem;
    private readonly ILogger _log;
    private readonly DiscoveryOptions _options;
    private readonly Assembly _testAssembly;

    public CommandFactory(
        ILogger log,
        IFileSystem fileSystem,
        DiscoveryOptions options,
        Assembly testAssembly,
        IAnalyticsContainer analytics)
    {
        _log = log;
        _fileSystem = fileSystem;
        _options = options;
        _testAssembly = testAssembly;
        _analytics = analytics;
    }

    public DiscoveryCommand CreateCommand() =>
        _options
            .Tie(AttachDebuggerWhenRequired)
            .Map(ToCommand);

    public static void AttachDebuggerWhenRequired(ConnectorOptions connectorOptions)
    {
        if (connectorOptions.DebugMode && !Debugger.IsAttached)
            Debugger.Launch();
    }

    public DiscoveryCommand ToCommand(DiscoveryOptions options) =>
        new(
            options.ConfigFile?.Map(FileDetails.FromPath) ?? None<FileDetails>.Value,
            _log,
            _fileSystem,
            _testAssembly,
            _analytics);
}
