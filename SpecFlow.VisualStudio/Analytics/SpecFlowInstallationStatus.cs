#nullable disable

namespace SpecFlow.VisualStudio.Analytics;

public class SpecFlowInstallationStatus
{
    public bool IsInstalled => InstalledVersion != null;
    public bool Is2019Installed => Installed2019Version != null;
    public Version InstalledVersion { get; set; }
    public Version Installed2019Version { get; set; }
    public DateTime? InstallDate { get; set; }
    public DateTime? LastUsedDate { get; set; }
    public int UsageDays { get; set; }
    public int UserLevel { get; set; }
}
