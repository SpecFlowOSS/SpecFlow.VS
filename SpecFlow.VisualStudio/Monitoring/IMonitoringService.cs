using System;
using SpecFlow.VisualStudio.ProjectSystem;
using SpecFlow.VisualStudio.ProjectSystem.Settings;
using SpecFlow.VisualStudio.UI.ViewModels;

namespace SpecFlow.VisualStudio.Monitoring
{
    public interface IMonitoringService
    {
        void MonitorLoadProjectSystem(string vsVersion);
        void MonitorOpenProjectSystem(string vsVersion, IIdeScope ideScope);
        void MonitorOpenProject(ProjectSettings settings, int? featureFileCount);
        void MonitorOpenFeatureFile(ProjectSettings projectSettings);

        void MonitorParserParse(int parseCount, int scenarioDefinitionCount);

        void MonitorCommandCommentUncomment();
        void MonitorCommandDefineSteps(CreateStepDefinitionsDialogResult action, int snippetCount);
        void MonitorCommandFindStepDefinitionUsages(int usagesCount, bool isCancelled);
        void MonitorCommandGoToStepDefinition(bool generateSnippet);
        void MonitorCommandAutoFormatTable();
        void MonitorCommandAddFeatureFile(ProjectSettings projectSettings);

        void MonitorSpecFlowDiscovery(bool isFailed, string errorMessage, int stepDefinitionCount, ProjectSettings projectSettings);
        void MonitorSpecFlowGeneration(bool isFailed, ProjectSettings projectSettings);

        void MonitorError(Exception exception, bool? isFatal = null);
    }
}
