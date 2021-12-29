namespace SpecFlow.VisualStudio.UI.ViewModels;

public class UpgradeDialogViewModel : WizardViewModel
{
    public const string UPGRADE_HEADER_TEMPLATE = @"
# SpecFlow Updated to v{newVersion}

Please have a look at the changes since the last installed version.

";

    public const string COMMUNITY_INFO_HEADER = @"
# Join the SpecFlow community!

";

    public const string COMMUNITY_INFO_TEXT = COMMUNITY_INFO_HEADER + @"
## Find solutions, share ideas and engage in discussions.

* Join our community forum: https://support.specflow.org/

* Join our Discord channel: https://discord.com/invite/xQMrjDXx7a

* Follow us on Twitter: https://twitter.com/specflow

* Follow us on LinkedIn: https://www.linkedin.com/company/specflow/

* Subscribe on YouTube: https://www.youtube.com/c/SpecFlowBDD

* Join our next webinar: https://specflow.org/community/webinars/

In case you are missing an important feature, please leave us your feature request [here](https://support.specflow.org/hc/en-us/community/topics/360000519178-Feature-Requests).
";

#if DEBUG
    public static UpgradeDialogViewModel DesignData = new("1.0.99", @"# v1.0.1 - 2019-02-27

Bug fixes:

* CreatePersistentTrackingPosition Exception / Step navigation error
* .NET Core Bindings: Unable to load BoDi.dll (temporary fix)

");
#endif

    public UpgradeDialogViewModel(string newVersion, string changeLog) : base("Close", "Welcome to SpecFlow",
        new MarkDownWizardPageViewModel("Changes")
        {
            Text = GetChangesText(newVersion, changeLog)
        },
        new MarkDownWizardPageViewModel("Community")
        {
            Text = COMMUNITY_INFO_TEXT
        })
    {
    }

    public MarkDownWizardPageViewModel OtherNewsPage =>
        Pages.OfType<MarkDownWizardPageViewModel>().FirstOrDefault(p => p.Name == "Community");

    private static string GetChangesText(string newVersion, string changeLog)
    {
        changeLog = Regex.Replace(changeLog, @"^#\s", m => "#" + m.Value, RegexOptions.Multiline);
        return UPGRADE_HEADER_TEMPLATE.Replace("{newVersion}", newVersion) + Environment.NewLine + changeLog;
    }
}
