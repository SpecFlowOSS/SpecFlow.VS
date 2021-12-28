namespace SpecFlow.VisualStudio.Diagnostics;

public static class DeveroomLoggerExtensions
{
    public static bool IsLogging(this IDeveroomLogger logger, TraceLevel messageLevel)
        => messageLevel <= logger.Level;

    public static void LogWarning(this IDeveroomLogger logger, string message,
        [CallerMemberName] string callerName = "???")
    {
        var msg = new LogMessage(TraceLevel.Warning, message, DateTimeOffset.Now, callerName);
        logger.Log(msg);
    }

    public static void LogInfo(this IDeveroomLogger logger, string message,
        [CallerMemberName] string callerName = "???")
    {
        var msg = new LogMessage(TraceLevel.Info, message, DateTimeOffset.Now, callerName);
        logger.Log(msg);
    }

    public static void LogVerbose(this IDeveroomLogger logger, string message,
        [CallerMemberName] string callerName = "???")
    {
        var msg = new LogMessage(TraceLevel.Verbose, message, DateTimeOffset.Now, callerName);
        logger.Log(msg);
    }

    public static void LogVerbose(this IDeveroomLogger logger, Func<string> message,
        [CallerMemberName] string callerName = "???")
    {
        if (!logger.IsLogging(TraceLevel.Verbose)) return;

        var msg = new LogMessage(TraceLevel.Verbose, message(), DateTimeOffset.Now, callerName);
        logger.Log(msg);
    }

    public static void LogException(this IDeveroomLogger logger, IMonitoringService monitoringService, Exception ex,
        string message = "Exception", [CallerMemberName] string callerName = "???")
    {
        monitoringService?.MonitorError(ex);
        var msg = new LogMessage(TraceLevel.Error, message, DateTimeOffset.Now, callerName, ex);
        logger.Log(msg);
    }

    public static void LogVerboseException(this IDeveroomLogger logger, IMonitoringService monitoringService,
        Exception ex, string message = "Exception", [CallerMemberName] string callerName = "???")
    {
        monitoringService.MonitorError(ex, false);
        var msg = new LogMessage(TraceLevel.Verbose, message, DateTimeOffset.Now, callerName, ex);
        logger.Log(msg);
    }

    //TODO: merge IDeveroomLogger with IMonitoringService
    public static void LogDebugException(this IDeveroomLogger logger, Exception ex, string message = "Exception",
        [CallerMemberName] string callerName = "???")
    {
        var msg = new LogMessage(TraceLevel.Verbose, message, DateTimeOffset.Now, callerName, ex);
        logger.Log(msg);
    }
}
