using System.Reflection;

namespace SpecFlow.VisualStudio.SpecFlowConnector;

public record ConnectorOptions(bool DebugMode)
{
    public static Either<Exception, ConnectorOptions> Parse(string[] args)
    {
        if (args.Length == 0)
            return new ArgumentException("Command is missing!");

        var commandArgsList = args.Skip(1).ToList();
        int debugArgIndex = commandArgsList.IndexOf("--debug");
        var debugMode = false;
        if (debugArgIndex >= 0)
        {
            debugMode = true;
            commandArgsList.RemoveAt(debugArgIndex);
        }

        var commandName = args[0];
        var commandArgs = commandArgsList.ToArray();
        switch (commandName)
        {
            case DiscoveryCommand.CommandName:
                return DiscoveryOptions.Parse(commandArgs, debugMode);
            default: return new ArgumentException($"Invalid command: {commandName}");
        }
    }
}

public record DiscoveryOptions(
    bool DebugMode,
    FileDetails AssemblyFile,
    Option<FileDetails> ConfigFilePath,
    DirectoryInfo ConnectorFolder,
    string TargetFolder
) : ConnectorOptions(DebugMode)
{
    public static Either<Exception, ConnectorOptions> Parse(string[] args, bool debugMode)
    {
        if (args.Length < 1)
            return new InvalidOperationException("Usage: discovery <test-assembly-path> [<config-file-path>]");

        var assemblyFile = FileDetails.FromPath(args[0]);
        Option<FileDetails> configFilePath = args.Length < 2 || string.IsNullOrWhiteSpace(args[1])
            ? None.Value
            : FileDetails.FromPath(args[1]);

        var TargetFolder = Path.GetDirectoryName(assemblyFile);
        if (TargetFolder == null)
            return new InvalidOperationException(
                $"Unable to detect target folder from test assembly path '{assemblyFile}'");

        return typeof(Runner)
            .Assembly
            .GetLocalCodeBase()
            .Map(Directory)
            .Map(connectorFolder =>
                new DiscoveryOptions(debugMode, assemblyFile, configFilePath, connectorFolder, TargetFolder)
                    as ConnectorOptions
            );
    }

    private static Either<Exception, DirectoryInfo> Directory(FileDetails fileDetails)
    {
        return fileDetails.Directory.Map<Exception, DirectoryInfo>(() =>
            new InvalidOperationException("Unable to detect connector folder."));
    }
}
