using System;
using System.Linq;
using System.Text.RegularExpressions;
using Deveroom.VisualStudio.UI.ViewModels.WizardDialogs;

namespace Deveroom.VisualStudio.UI.ViewModels
{
    public class WhatsNewDialogViewModel : WizardViewModel
    {
        public const string UPGRADE_HEADER_TEMPLATE = @"
# Deveroom Updated to v{newVersion}

Please have a look at the changes since the last installed version.

";

        public const string ACTUAL_INFO_HEADER = @"
# This might be also interesting for you...

";

        public const string ACTUAL_INFO_TEXT = ACTUAL_INFO_HEADER + @"
## BDD Addict Newsletter

If you would like to keep yourself up-to-date with news about BDD, SpecFlow, test automation or agile testing, subscribe to 
the **BDD Addict Newsletter**
where we share interesting blog posts, atricles and videos from the community every month. 

Check out the past issues and subscribe at http://bddaddict.com.
";

        public MarkDownWizardPageViewModel OtherNewsPage => Pages.OfType<MarkDownWizardPageViewModel>().FirstOrDefault(p => p.Name == "Other News");

        public WhatsNewDialogViewModel(string newVersion, string changeLog) : base("Close", "Welcome to Deveroom",
            new MarkDownWizardPageViewModel("Changes")
            {
                Text = GetChangesText(newVersion, changeLog)
            },
            new MarkDownWizardPageViewModel("Other News")
            {
                Text = ACTUAL_INFO_TEXT
            })
        {
        }

        private static string GetChangesText(string newVersion, string changeLog)
        {
            changeLog = Regex.Replace(changeLog, @"^#\s", m => "#" + m.Value, RegexOptions.Multiline);
            return UPGRADE_HEADER_TEMPLATE.Replace("{newVersion}", newVersion) + Environment.NewLine + changeLog;
        }

#if DEBUG
        public static WhatsNewDialogViewModel DesignData = new WhatsNewDialogViewModel("1.0.99", @"# v1.0.1 - 2019-02-27

Bug fixes:

* CreatePersistentTrackingPosition Exception / Step navigation error
* .NET Core Bindings: Unable to load BoDi.dll (temporary fix)

");
#endif
    }
}
