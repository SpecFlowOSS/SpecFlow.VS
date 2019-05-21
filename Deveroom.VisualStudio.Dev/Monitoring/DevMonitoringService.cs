using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using Deveroom.VisualStudio.Monitoring;
using Deveroom.VisualStudio.ProjectSystem;
using Deveroom.VisualStudio.ProjectSystem.Settings;
using Deveroom.VisualStudio.UI.ViewModels;

namespace Deveroom.VisualStudio.Dev.Monitoring
{
    [Export(typeof(IMonitoringService))]
    public class DevMonitoringService : IMonitoringService
    {
        private void Monitor(object[] args = null, [CallerMemberName] string name = "Unknown")
        {
            args = args ?? new object[0];
            Debug.WriteLine(string.Join(",", args.Select(a => a?.ToString() ?? "<null>")), $"Deveroom:{name}");
        }

        public void MonitorLoadProjectSystem(string vsVersion)
        {
            Monitor(new object[]{ vsVersion });
        }

        public void MonitorOpenProjectSystem(string vsVersion, IIdeScope ideScope)
        {
            Monitor(new object[] { vsVersion });
        }

        public void MonitorOpenProject(ProjectSettings settings, int? featureFileCount)
        {
            Monitor(new object[] { settings, featureFileCount });
        }

        public void MonitorOpenFeatureFile(ProjectSettings projectSettings)
        {
            Monitor(new object[] { projectSettings });
        }

        public void MonitorParserParse(int parseCount, int scenarioDefinitionCount)
        {
            Monitor(new object[] { parseCount, scenarioDefinitionCount });
        }

        public void MonitorCommandCommentUncomment()
        {
            Monitor();
        }

        public void MonitorCommandDefineSteps(CreateStepDefinitionsDialogResult action, int snippetCount)
        {
            Monitor(new object[] { action, snippetCount });
        }

        public void MonitorCommandFindStepDefinitionUsages(int usagesCount, bool isCancelled)
        {
            Monitor(new object[] { usagesCount, isCancelled });
        }

        public void MonitorCommandGoToStepDefinition(bool generateSnippet)
        {
            Monitor(new object[] { generateSnippet });
        }

        public void MonitorCommandAutoFormatTable()
        {
            Monitor();
        }

        public void MonitorCommandAddFeatureFile(ProjectSettings projectSettings)
        {
            Monitor(new object[] { projectSettings });
        }

        public void MonitorSpecFlowDiscovery(bool isFailed, string errorMessage, int stepDefinitionCount, ProjectSettings projectSettings)
        {
            Monitor(new object[] { isFailed, errorMessage, stepDefinitionCount, projectSettings });
        }

        public void MonitorSpecFlowGeneration(bool isFailed, ProjectSettings projectSettings)
        {
            Monitor(new object[] { isFailed, projectSettings });
        }

        public void MonitorError(Exception exception, bool? isFatal = null)
        {
            Monitor(new object[] { exception, isFatal });
        }
    }
}
