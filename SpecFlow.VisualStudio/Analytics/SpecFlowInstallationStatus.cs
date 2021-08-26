using System;

namespace SpecFlow.VisualStudio.Analytics
{
    public class SpecFlowInstallationStatus
    {
        public bool IsInstalled => InstalledVersion != null;
        public Version InstalledVersion { get; set; }
        public DateTime? InstallDate { get; set; }
        public DateTime? LastUsedDate { get; set; }
        public int UsageDays { get; set; }
        public int UserLevel { get; set; }
    }
}