using System;
using System.Diagnostics;

namespace Deveroom.VisualStudio.EventTracking
{
    public class AnalyticsApi : IAnalyticsApi
    {
        public bool HasClientId
        {
            get
            {
                //TODO: return RegistryManager.GetStringValue("cid") != null;
                return false;
            }
        }
        
        public bool LogEnabled { get; }
        public Exception SenderError { get; }

        public AnalyticsApi()
        {
            var devMode = IsDevMode();
            LogEnabled = devMode;

            //TODO: Create sender, get SenderError
        }

        public void Log(object message)
        {
            if (!LogEnabled || message == null)
                return;

            if (message is Exception exceptionMessage)
            {
                message = "Error: " + EventTracker.GetExceptionMessage(exceptionMessage);
            }

            Debug.WriteLine(message, GetType().FullName);
        }

        private bool IsDevMode()
        {
            return true; //TODO
        }
        
        public void TrackEvent(string category, string action, string label, int? value = null)
        {
            //TODO
        }

        public void TrackException(string exceptionDetail, bool isFatal)
        {
            //TODO
        }
    }
}