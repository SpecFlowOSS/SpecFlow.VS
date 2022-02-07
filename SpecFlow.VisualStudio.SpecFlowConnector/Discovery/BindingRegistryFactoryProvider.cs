namespace SpecFlowConnector.Discovery;

public class BindingRegistryFactoryProvider
{
    private readonly ILogger _log;
    private readonly Assembly _testAssembly;
    private readonly IFileSystem _fileSystem;

    public BindingRegistryFactoryProvider(ILogger log, Assembly testAssembly, IFileSystem fileSystem)
    {
        _log = log;
        _testAssembly = testAssembly;
        _fileSystem = fileSystem;
    }

    public IBindingRegistryFactory Create()
    {
        var versionNumber = GetSpecFlowVersion();
        var factory = GetFactory(versionNumber);
        _log.Debug($"Chosen {factory.GetType().Name} for {versionNumber}");
        return factory;
    }

    private IBindingRegistryFactory GetFactory(int versionNumber)
    {
        return versionNumber switch
        {
            //>= 3_07_013 => new SpecFlowDiscoverer(),
            //>= 3_00_000 => new SpecFlowV30Discoverer(),
            //>= 2_02_000 => new SpecFlowV22Discoverer(),
            //>= 2_01_000 => new SpecFlowV21Discoverer(),
            //>= 2_00_000 => new SpecFlowV20Discoverer(),
            _ => new BindingRegistryFactoryV3922(_fileSystem)
        };
    }

    private int GetSpecFlowVersion()
    {
        //C:\Users\santa\AppData\Local\Temp\Deveroom\DS_GPT_3.9.40_nunit_nprj_net6.0_bt_1194832604\bin\Debug\net6.0\TechTalk.SpecFlow.dll
        var specFlowAssemblyPath = Path.Combine(Path.GetDirectoryName(_testAssembly.Location) ?? ".", "TechTalk.SpecFlow.dll");
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
