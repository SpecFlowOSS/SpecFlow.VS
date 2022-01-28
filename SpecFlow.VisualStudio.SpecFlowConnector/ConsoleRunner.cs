﻿namespace SpecFlow.VisualStudio.SpecFlowConnector;

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
                .Tie(PrintException)
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

    private static void PrintResult(CommandResult result)
    {
        Log.Info(JsonSerialization.MarkResult(result.Json));
    }

    private static void PrintException(Exception ex)
    {
        Log.Error(Dump(ex));
    }

    public static string Dump(Exception ex)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Error: {GetExceptionMessage(ex)}");
        sb.AppendLine($"Exception: {GetExceptionTypes(ex)}");
        sb.Append(GetStackTrace(ex));
        return sb.ToString();
    }

    public static string GetExceptionTypes(Exception ex)
    {
        var exceptionTypes = ex.GetType().FullName;
        if (ex.InnerException != null)
            exceptionTypes = $"{exceptionTypes}->{GetExceptionTypes(ex.InnerException)}";
        return exceptionTypes;
    }

    private static string GetExceptionMessage(Exception ex)
    {
        var message = ex.Message;
        if (ex.InnerException != null)
            message = $"{message} -> {GetExceptionMessage(ex.InnerException)}";
        return message;
    }

    private static string GetStackTrace(Exception ex)
    {
        var stackTrace = "";
        while (ex != null)
        {
            stackTrace = "StackTrace of " + ex.GetType().Name + ":" + Environment.NewLine + ex.StackTrace +
                         Environment.NewLine + stackTrace;
            ex = ex.InnerException;
        }

        return stackTrace;
    }

    private static int ToResultCode(Exception ex) => ex is ArgumentException ? 3 : 4;
}
