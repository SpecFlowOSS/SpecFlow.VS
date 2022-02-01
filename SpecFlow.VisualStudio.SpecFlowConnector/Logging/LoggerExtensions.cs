﻿namespace SpecFlow.VisualStudio.SpecFlowConnector;

public static class LoggerExtensions
{
    public static void Error(this ILogger logger, string message)
    {
        logger.Log(new Log(LogLevel.Error, message));
    }

    public static void Info(this ILogger logger, string message)
    {
        logger.Log(new Log(LogLevel.Info, message));
    }

    public static void Debug(this ILogger logger, string message)
    {
        logger.Log(new Log(LogLevel.Debug, message));
    }
}
