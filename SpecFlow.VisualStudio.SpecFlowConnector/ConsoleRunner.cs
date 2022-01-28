namespace SpecFlow.VisualStudio.SpecFlowConnector;

public class ConsoleRunner
{
    public ConsoleRunner(ILogger log)
    {
        Log = log;
    }

    private ILogger Log { get; }

    public int EntryPoint(string[] args)
    {
        try
        {
            return args
                .Map(ConnectorOptions.Parse)
                .AttachDebuggerWhenRequired()
                .Map(ToCommand)
                .Execute()
                .Tie(PrintResult)
                .Map(result => result.Code);
        }
        catch (Exception ex)
        {
            return ex
                .Tie(e => Log.Error(e.Dump()))
                .Map(ToResultCode);
        }
    }

    public static ICommand ToCommand(ConnectorOptions options)
    {
        switch (options.CommandName)
        {
            case DiscoveryCommand.CommandName: return new DiscoveryCommand(options);
            //case GeneratorCommand.CommandName: return new GeneratorCommand(options);
            default: throw new ArgumentException($"Invalid command: {options.CommandName}");
        }
    }

    private void PrintResult(CommandResult result)
    {
        Log.Info(JsonSerialization.MarkResult(result.Json));
    }

    private static int ToResultCode(Exception ex) => ex is ArgumentException ? 3 : 4;
}
