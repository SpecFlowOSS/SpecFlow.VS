using System;
using System.Diagnostics;

namespace SpecFlow.VisualStudio.EventTracking
{
    public class AnalyticsApi : IAnalyticsApi
    {
        public bool HasClientId => RegistryManager.GetStringValue("cid") != null;

        private static string GetClientId()
        {
            var cid = RegistryManager.GetStringValue("cid");
            if (cid == null)
            {
                cid = Guid.NewGuid().ToString("D");
                RegistryManager.SetString("cid", cid);
            }
            return cid;
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