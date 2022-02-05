using SpecFlow.VisualStudio.SpecFlowConnector.Tests;

namespace SpecFlowConnector;

public class Runner
{
    private readonly ILogger _log;

    public Runner(ILogger log)
    {
        _log = log;
    }

    public int Run(string[] args, Func<string, TestAssemblyLoadContext> testAssemblyLoadContext, IFileSystem fileSystem)
    {
        var internalLogger =
#if DEBUG
            _log;
#else
        new StringBuilderLogger();
#endif
        try
        {
            return new CommandFactory(internalLogger, fileSystem)
                .CreateCommand(args)
                .Map(cmd => cmd.Execute(testAssemblyLoadContext))
                .Tie(PrintResult)
                .Map<Exception, CommandResult,int>(_ => 0)
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
