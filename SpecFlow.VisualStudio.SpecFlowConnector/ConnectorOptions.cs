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