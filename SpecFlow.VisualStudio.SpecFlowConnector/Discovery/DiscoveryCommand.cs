namespace SpecFlowConnector.Discovery;

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
        IBindingRegistryFactory bindingRegistryFactory = new BindingRegistryFactoryProvider(_log, _options, _fileSystem)
            .Create();

        _log.Debug($"Loading {_options.AssemblyFile.FullName}");
        var assembly = assemblyFromPath(_options.AssemblyFile.FullName);
        _log.Debug($"Loaded: {assembly}");

        return new SpecFlowDiscoverer()
            .Discover(bindingRegistryFactory, assembly, _options.ConfigFile)
            .Map(dr=>new CommandResult(JsonSerialization.SerializeObject(dr)));
    }
}

