namespace SpecFlowConnector.Discovery;

public class BindingRegistryFactoryProvider
{
    private readonly IAnalyticsContainer _analytics;
    private readonly IFileSystem _fileSystem;
    private readonly ILogger _log;
    private readonly Assembly _testAssembly;

    public BindingRegistryFactoryProvider(
        ILogger log,
        Assembly testAssembly,
        IFileSystem fileSystem,
        IAnalyticsContainer analytics)
    {
        _log = log;
        _testAssembly = testAssembly;
        _fileSystem = fileSystem;
        _analytics = analytics;
    }

    public IBindingRegistryFactory Create()
    {
        var versionNumber = GetSpecFlowVersion();
        var factory = GetFactory(versionNumber);
        _log.Info($"Chosen {factory.GetType().Name} for {versionNumber}");
        return factory;
    }

    private IBindingRegistryFactory GetFactory(int versionNumber)
    {
        return versionNumber switch
        {
            >= 3_09_022 => new BindingRegistryFactoryVLatest(_fileSystem),
            >= 3_07_013 => new BindingRegistryFactoryBeforeV309022(_fileSystem),
            >= 3_00_213 => new BindingRegistryFactoryBeforeV307013(_fileSystem),
            _ => new BindingRegistryFactoryBeforeV300213(_fileSystem)
        };
    }

    private int GetSpecFlowVersion()
    {
        var specFlowAssemblyPath =
            Path.Combine(Path.GetDirectoryName(_testAssembly.Location) ?? ".", "TechTalk.SpecFlow.dll");
        if (File.Exists(specFlowAssemblyPath))
            return GetSpecFlowVersion(specFlowAssemblyPath);

        foreach (var otherSpecFlowFile in Directory.EnumerateFiles(
                     Path.GetDirectoryName(specFlowAssemblyPath)!, "*SpecFlow*.dll"))
        {
            var ver = GetSpecFlowVersion(otherSpecFlowFile);
            if (ver >= 2_00_000) return ver;
        }

        _log.Info($"Not found {specFlowAssemblyPath}");
        _analytics.AddAnalyticsProperty("SFFile", "Not found");
        return int.MaxValue;
    }

    private int GetSpecFlowVersion(string specFlowAssemblyPath)
    {
        var specFlowVersion = FileVersionInfo.GetVersionInfo(specFlowAssemblyPath);
        var versionNumber = (specFlowVersion.FileMajorPart * 100 +
                             specFlowVersion.FileMinorPart) * 1000 +
                            specFlowVersion.FileBuildPart;

        _log.Info($"Found SpecFlow V{versionNumber} at {specFlowAssemblyPath}");
        _analytics.AddAnalyticsProperty("SFFile", specFlowVersion.InternalName ?? specFlowVersion.FileName);
        _analytics.AddAnalyticsProperty("SFFileVersion", specFlowVersion.FileVersion ?? "Unknown");
        _analytics.AddAnalyticsProperty("SFProductVersion", specFlowVersion.ProductVersion ?? "Unknown");

        return versionNumber;
    }
}
