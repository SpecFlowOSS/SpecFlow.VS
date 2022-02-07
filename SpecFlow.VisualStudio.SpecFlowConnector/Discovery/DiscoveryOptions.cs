namespace SpecFlowConnector.Discovery;

public record DiscoveryOptions(
    bool DebugMode,
    string AssemblyFile,
    string? ConfigFile,
    string ConnectorFolder
) : ConnectorOptions(DebugMode)
{
    public static Either<Exception, ConnectorOptions> Parse(string[] args, bool debugMode)
    {
        return args
            .Map(ValidateParameterCount)
            .Map(validArgs => (testAssemblyFile: AssemblyPath(validArgs), configFile: ConfigPath(validArgs)))
            .Map(ValidateTargetFolder)
            .Map(GetConnectorFolder)
            .Map(parsed => new DiscoveryOptions(
                debugMode,
                parsed.targetAssemblyFile.FullName,
                parsed.configFile.Map(file=>file.FullName).Reduce((string?)null!),
                parsed.connectorFolder.FullName
            ) as ConnectorOptions);
    }

    private static Either<Exception, string[]> ValidateParameterCount(string[] args) => args.Length >= 1
        ? args
        : new InvalidOperationException("Usage: discovery <test-assembly-path> [<configuration-file-path>]");

    private static FileDetails AssemblyPath(string[] args) => FileDetails.FromPath(args[0]);

    private static Option<FileDetails> ConfigPath(string[] args) =>
        args.Length < 2 || string.IsNullOrWhiteSpace(args[1])
            ? None.Value
            : FileDetails.FromPath(args[1]);

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
