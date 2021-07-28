using System;
using System.Linq;

namespace Deveroom.VisualStudio.EventTracking
{
    public interface IAnalyticsApi
    {
        bool HasClientId { get; }
        bool LogEnabled { get; }
        Exception SenderError { get; }
        void Log(object message);
        void TrackEvent(string category, string action, string label, int? value = null);
        void TrackException(string exceptionDetail, bool isFatal);
    }
}
