﻿namespace SpecFlowConnector.Discovery;

public class BindingRegistryFactoryProvider
{
    private readonly ILogger _log;
    private readonly DiscoveryOptions _options;
    private readonly IFileSystem _fileSystem;

    public BindingRegistryFactoryProvider(ILogger log, DiscoveryOptions options, IFileSystem fileSystem)
    {
        _log = log;
        _options = options;
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