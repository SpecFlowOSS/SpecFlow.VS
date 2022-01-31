namespace SpecFlow.VisualStudio.SpecFlowConnector;

public class Runner
{
    private readonly ILogger _log;

    public Runner(ILogger log)
    {
        _log = log;
    }

    public int Run(string[] args)
    {
        try
        {
            return CommandFactory
                .CreateCommand(args)
                .Map(cmd => cmd.Execute())
                .Tie(PrintResult)
                .Map(result => result.Code)
                .Reduce(HandleException);
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    private void PrintResult(CommandResult result)
    {
        _log.Info(JsonSerialization.MarkResult(result.Json));
    }

    private int HandleException(Exception ex)
    {
        return ex.Tie(e => _log.Error(e.Dump()))
            .Map(e => e is ArgumentException ? 3 : 4);
    }
}
