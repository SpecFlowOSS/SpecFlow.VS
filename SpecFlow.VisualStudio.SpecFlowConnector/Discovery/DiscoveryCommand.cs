namespace SpecFlow.VisualStudio.SpecFlowConnector.Discovery;

public class DiscoveryCommand : ICommand
{
    public const string CommandName = "discovery";
    private readonly DiscoveryOptions _options;
    private readonly ILogger _log;
    private readonly IFileSystem _fileSystem;

    public DiscoveryCommand(DiscoveryOptions options, ILogger log, IFileSystem fileSystem)
    {
        _options = options;
        _log = log;
        _fileSystem = fileSystem;
    }

    public CommandResult Execute(Func<string, Assembly> assemblyFromPath)
    {
        var discoverer = new SpecFlowDiscovererProvider(_options, _log, _fileSystem)
            .GetDiscoverer();

        var assembly = assemblyFromPath(_options.AssemblyFile.FullName);
        return new("{}");
    }
}
