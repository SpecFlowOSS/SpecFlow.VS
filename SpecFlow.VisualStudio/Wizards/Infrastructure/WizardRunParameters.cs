using System.Collections.Generic;
using SpecFlow.VisualStudio.Monitoring;
using SpecFlow.VisualStudio.ProjectSystem;

namespace SpecFlow.VisualStudio.Wizards.Infrastructure
{
    public class WizardRunParameters
    {
        public const string CustomToolSettingKey = "$customtool$";
        public const string BuildActionKey = "$buildaction$";

        public bool IsAddNewItem { get; }
        public IProjectScope ProjectScope { get; }
        public string TemplateFolder { get; }
        public string TargetFolder { get; }
        public string TargetFileName { get; }
        public Dictionary<string, string> ReplacementsDictionary { get; }

        public IMonitoringService MonitoringService => ProjectScope.IdeScope.MonitoringService;

        public WizardRunParameters(bool isAddNewItem, IProjectScope projectScope, string templateFolder, string targetFolder, string targetFileName, Dictionary<string, string> replacementsDictionary)
        {
            IsAddNewItem = isAddNewItem;
            ProjectScope = projectScope;
            TemplateFolder = templateFolder;
            TargetFolder = targetFolder;
            TargetFileName = targetFileName;
            ReplacementsDictionary = replacementsDictionary;
        }
    }
}