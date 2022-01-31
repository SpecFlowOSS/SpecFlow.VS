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
    string AssemblyFilePath,
    string ConfigFilePath,
    string ConnectorFolder,
    string TargetFolder
) : ConnectorOptions(DebugMode)
{
    public static Either<Exception, ConnectorOptions> Parse(string[] args, bool debugMode)
    {
        if (args.Length < 1)
            throw new InvalidOperationException("Usage: discovery <test-assembly-path> [<config-file-path>]");

        var assemblyFilePath = FileDetails.FromPath(args[0]);

        var AssemblyFilePath = Path.GetFullPath(args[0]);
        if (assemblyFilePath.FullName != AssemblyFilePath) throw new InvalidOperationException("nemjó");

        var ConfigFilePath = args.Length < 2 || string.IsNullOrWhiteSpace(args[1]) ? null : Path.GetFullPath(args[1]);

        var TargetFolder = Path.GetDirectoryName(AssemblyFilePath);
        if (TargetFolder == null)
            return new InvalidOperationException(
                $"Unable to detect target folder from test assembly path '{AssemblyFilePath}'");

        string ConnectorFolder = null;// Path.GetDirectoryName(typeof(Runner).Assembly.GetLocalCodeBase());
        if (ConnectorFolder == null)
            return new InvalidOperationException("Unable to detect connector folder.");

        return new DiscoveryOptions(debugMode, AssemblyFilePath, ConfigFilePath, ConnectorFolder, TargetFolder);
    }
}
