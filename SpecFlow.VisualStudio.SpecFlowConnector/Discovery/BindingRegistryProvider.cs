namespace SpecFlowConnector.Discovery;

public class BindingRegistryProvider
{
    private readonly ILogger _log;
    private readonly DiscoveryOptions _options;
    private readonly IFileSystem _fileSystem;

    public BindingRegistryProvider(ILogger log, DiscoveryOptions options, IFileSystem fileSystem)
    {
        _log = log;
        _options = options;
        _fileSystem = fileSystem;
    }

    public IBindingRegistryProxy GetBindingRegistry()
    {
        var versionNumber = GetSpecFlowVersion();
        var bindingRegistry = GetBindingRegistry(versionNumber);
        _log.Debug($"Chosen {bindingRegistry.GetType().Name} for {versionNumber}");
        return bindingRegistry;
    }

    private IBindingRegistryProxy GetBindingRegistry(int versionNumber)
    {
        return versionNumber switch
        {
            //>= 3_07_013 => new SpecFlowDiscoverer(),
            //>= 3_00_000 => new SpecFlowV30Discoverer(),
            //>= 2_02_000 => new SpecFlowV22Discoverer(),
            //>= 2_01_000 => new SpecFlowV21Discoverer(),
            //>= 2_00_000 => new SpecFlowV20Discoverer(),
            _ => new BindingRegistryProxyV3_9_22(_fileSystem)
        };
    }

    private int GetSpecFlowVersion()
    {
        var specFlowAssemblyPath = Path.Combine(_options.AssemblyFile.DirectoryName.Reduce("."), "TechTalk.SpecFlow.dll");
        if (_fileSystem.File.Exists(specFlowAssemblyPath))
        {
            var specFlowVersion = FileVersionInfo.GetVersionInfo(specFlowAssemblyPath);
            var versionNumber = (specFlowVersion.FileMajorPart * 100 + specFlowVersion.FileMinorPart) * 1000 +
                                specFlowVersion.FileBuildPart;
            _log.Debug($"Found SpecFlow V{versionNumber} at {specFlowAssemblyPath}");
            return versionNumber;
        }
        _log.Debug($"Not found {specFlowAssemblyPath}");
        return int.MaxValue;
    }
}
