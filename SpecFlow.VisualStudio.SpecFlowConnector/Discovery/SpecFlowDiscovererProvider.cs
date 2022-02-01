namespace SpecFlow.VisualStudio.SpecFlowConnector.Discovery;

public class SpecFlowDiscovererProvider
{
    private DiscoveryOptions _options;
    private readonly ILogger _log;
    private IFileSystem _fileSystem; 

    public SpecFlowDiscovererProvider(DiscoveryOptions options, ILogger log, IFileSystem fileSystem)
    {
        _options = options;
        _log = log;
        _fileSystem = fileSystem;
    }

    public ISpecFlowDiscoverer GetDiscoverer()
    {
        var versionNumber = GetSpecFlowVersion();
        var discoverer= GetDiscoverer(versionNumber);
        _log.Debug($"Chosen {discoverer.GetType().Name} for {versionNumber}");
        return discoverer;
    }

    private static ISpecFlowDiscoverer GetDiscoverer(int versionNumber)
    {
        return versionNumber switch
        {
            >= 3_07_013 => new SpecFlowVLatestDiscoverer(),
            //>= 3_00_000 => new SpecFlowV30Discoverer(),
            //>= 2_02_000 => new SpecFlowV22Discoverer(),
            //>= 2_01_000 => new SpecFlowV21Discoverer(),
            //>= 2_00_000 => new SpecFlowV20Discoverer(),
            _ => new SpecFlowV19Discoverer()
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
