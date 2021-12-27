namespace SpecFlow.VisualStudio.Analytics;

public class SpecFlowInstallationStatus
{
    public static readonly DateTime MagicDate = new(2009, 9, 11); //when SpecFlow has born
    public static readonly Version UnknownVersion = new(0, 0);
    public bool IsInstalled => InstalledVersion != UnknownVersion;
    public bool Is2019Installed => Installed2019Version != UnknownVersion;
    public Version InstalledVersion { get; set; } = UnknownVersion;
    public Version Installed2019Version { get; set; } = UnknownVersion;
    public DateTime InstallDate { get; set; }
    public DateTime LastUsedDate { get; set; }
    public int UsageDays { get; set; }
    public int UserLevel { get; set; }
}
