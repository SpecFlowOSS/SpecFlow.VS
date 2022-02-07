namespace SpecFlowConnector.Discovery;

public class DiscoveryCommand
{
    public const string CommandName = "discovery";
    private readonly Option<FileDetails> _configFile;
    private readonly ILogger _log;
    private readonly IFileSystem _fileSystem;
    private readonly Assembly _testAssembly;

    public DiscoveryCommand(Option<FileDetails> configFile, ILogger log, IFileSystem fileSystem, Assembly testAssembly)
    {
        _configFile = configFile;
        _log = log;
        _fileSystem = fileSystem;
        _testAssembly = testAssembly;
    }

    public DiscoveryResult Execute(AssemblyLoadContext assemblyLoadContext)
    {
        return new BindingRegistryFactoryProvider(_log, _testAssembly, _fileSystem)
            .Create()
            .Map(bindingRegistryFactory => new SpecFlowDiscoverer(_log)
                .Discover(bindingRegistryFactory, assemblyLoadContext, _testAssembly, _configFile));
    }
}

