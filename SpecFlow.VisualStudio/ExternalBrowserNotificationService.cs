using System;
using System.Diagnostics;
using System.Linq;
using SpecFlow.VisualStudio.Diagnostics;
using SpecFlow.VisualStudio.ProjectSystem;

namespace SpecFlow.VisualStudio
{
    public class ExternalBrowserNotificationService
    {
        private readonly IIdeScope _ideScope;

        public ExternalBrowserNotificationService(IIdeScope ideScope)
        {
            _ideScope = ideScope;
        }

        public bool ShowPage(string url)
        {
            if (!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
            {
                _ideScope.Logger.LogWarning($"Showing page for '{url}' skipped, because the machine is offline");
                return false;
            }

            try
            {
                Process.Start(url);
                return true;
            }
            catch (Exception ex)
            {
                _ideScope.Logger.LogException(_ideScope.MonitoringService, ex, $"Browser start error: {ex}");
                return false;
            }
        }
    }
}
