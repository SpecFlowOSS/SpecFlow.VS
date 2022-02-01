using SpecFlow.VisualStudio.SpecFlowConnector.Tests;

namespace SpecFlow.VisualStudio.SpecFlowConnector;

public class Runner
{
    private readonly ILogger _log;

    public Runner(ILogger log)
    {
        _log = log;
    }

    public int Run(string[] args, Func<string, Assembly> assemblyFromPath, IFileSystem fileSystem)
    {
        var internalLogger = new StringBuilderLogger();
        try
        {
            return new CommandFactory(internalLogger, fileSystem)
                .CreateCommand(args)
                .Map(cmd => cmd.Execute(assemblyFromPath))
                .Tie(PrintResult)
                .Map<Exception, CommandResult,int>(_ => 1)
                .Reduce(HandleException);
        }
        catch (Exception ex)
        {
            _log.Debug(internalLogger.ToString());
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
