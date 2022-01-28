namespace SpecFlow.VisualStudio.SpecFlowConnector;

public record ConnectorOptions(string CommandName, bool DebugMode)
{
    public static ConnectorOptions Parse(string[] args)
    {
        if (args == null || args.Length == 0)
            throw new ArgumentException("Command is missing!");

        var commandArgsList = args.Skip(1).ToList();
        int debugArgIndex = commandArgsList.IndexOf("--debug");
        var debugMode = false;
        if (debugArgIndex >= 0)
        {
            debugMode = true;
            commandArgsList.RemoveAt(debugArgIndex);
        }

        var commandArgs = commandArgsList.ToArray();
        return new ConnectorOptions(args[0], debugMode);
    }

    public ConnectorOptions AttachDebuggerWhenRequired()
    {
        if (DebugMode && !Debugger.IsAttached)
            Debugger.Launch();

        return this;
    }
}
