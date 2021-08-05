using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using SpecFlow.VisualStudio.Monitoring;

namespace SpecFlow.VisualStudio.Diagnostics
{
    public static class DeveroomLoggerExtensions
    {
        public static bool IsLogging(this IDeveroomLogger logger, TraceLevel messageLevel)
            => messageLevel <= logger.Level;

        public static void LogWarning(this IDeveroomLogger logger, string message, [CallerMemberName] string callerName = "???")
        {
            logger.Log(TraceLevel.Warning, $"{callerName}: {message}");
        }

        public static void LogInfo(this IDeveroomLogger logger, string message, [CallerMemberName] string callerName = "???")
        {
            logger.Log(TraceLevel.Info, $"{callerName}: {message}");
        }

        public static void LogVerbose(this IDeveroomLogger logger, string message, [CallerMemberName] string callerName = "???")
        {
            logger.Log(TraceLevel.Verbose, $"{callerName}: {message}");
        }

        public static void LogVerbose(this IDeveroomLogger logger, Func<string> message, [CallerMemberName] string callerName = "???")
        {
            if (logger.IsLogging(TraceLevel.Verbose))
                logger.Log(TraceLevel.Verbose, $"{callerName}: {message()}");
        }

        public static void LogException(this IDeveroomLogger logger, IMonitoringService monitoringService, Exception ex, string message = "Exception", [CallerMemberName] string callerName = "???")
        {
            monitoringService?.MonitorError(ex);
            logger.Log(TraceLevel.Error, $"{callerName}: {message}: {ex}");
        }

        public static void LogVerboseException(this IDeveroomLogger logger, IMonitoringService monitoringService, Exception ex, string message = "Exception", [CallerMemberName] string callerName = "???")
        {
            monitoringService.MonitorError(ex, isFatal: false);
            logger.Log(TraceLevel.Verbose, $"{callerName}: {message}: {ex}");
        }

        //TODO: merge IDeveroomLogger with IMonitoringService
        public static void LogDebugException(this IDeveroomLogger logger, Exception ex, string message = "Exception", [CallerMemberName] string callerName = "???")
        {
            logger.Log(TraceLevel.Verbose, $"{callerName}: {message}: {ex}");
        }
    }
}
