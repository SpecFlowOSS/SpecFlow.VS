namespace SpecFlow.VisualStudio.SpecFlowConnector.Discovery;

public record DiscoveryOptions(
    bool DebugMode,
    FileDetails AssemblyFile,
    Option<FileDetails> ConfigFile,
    DirectoryInfo ConnectorFolder
) : ConnectorOptions(DebugMode)
{
    public static Either<Exception, ConnectorOptions> Parse(string[] args, bool debugMode)
    {
        return args
            .Map(ValidateParameterCount)
            .Map(AssemblyPath)
            .Map(ConfigPath)
            .Map(ValidateTargetFolder)
            .Map(GetConnectorFolder)
            .Map(x=>new DiscoveryOptions(debugMode, x.targetAssemblyFile, x.configFile, x.connectorFolder) as ConnectorOptions);
    }

    private static Either<Exception, string[]> ValidateParameterCount(string[] args) => args.Length >= 1
        ? args
        : new InvalidOperationException("Usage: discovery <test-assembly-path> [<configuration-file-path>]");

    private static Either<Exception, (FileDetails targetAssemblyFile, string[] args)>
        AssemblyPath(string[] args)
    {
        return (FileDetails.FromPath(args[0]), args);
    }

    private static Either<Exception, (FileDetails targetAssemblyFile, Option<FileDetails> configFile)>
        ConfigPath((FileDetails , string[] ) x)
    {
        var (targetAssemblyFile, args) = x;
        return (targetAssemblyFile,
                args.Length < 2 || string.IsNullOrWhiteSpace(args[1])
                    ? None.Value
                    : FileDetails.FromPath(args[1])
            );
    }

    private static Either<Exception, (FileDetails targetAssemblyFile, Option<FileDetails> configFile)>
        ValidateTargetFolder((FileDetails , Option<FileDetails> ) x)
    {
        var (targetAssemblyFile, configFile) = x;
        if (targetAssemblyFile.Directory is Some<DirectoryInfo>) return x;
        return new InvalidOperationException(
            $"Unable to detect target folder from test assembly path '{targetAssemblyFile}'"); ;
    }

    private static Either<Exception, (FileDetails targetAssemblyFile, Option<FileDetails> configFile, DirectoryInfo
            connectorFolder)>
        GetConnectorFolder((FileDetails, Option<FileDetails>) x)
    {
        var (targetAssemblyFile, configFile) = x;
        return typeof(Runner)
            .Assembly
            .GetLocation()
            .Map(Directory)
            .Map(connectorFolder => (targetAssemblyFile, configFile, connectorFolder));
    }

    private static Either<Exception, DirectoryInfo> Directory(FileDetails fileDetails)
    {
        return fileDetails.Directory.Map<Exception, DirectoryInfo>(() =>
            new InvalidOperationException("Unable to detect connector folder."));
    }
}
