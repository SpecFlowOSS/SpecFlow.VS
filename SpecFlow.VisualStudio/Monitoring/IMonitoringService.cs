using System;
using System.Collections.Generic;
using SpecFlow.VisualStudio.ProjectSystem;
using SpecFlow.VisualStudio.ProjectSystem.Settings;
using SpecFlow.VisualStudio.UI.ViewModels;

namespace SpecFlow.VisualStudio.Monitoring
{
    public interface IMonitoringService
    {
        void MonitorLoadProjectSystem();
        void MonitorOpenProjectSystem(IIdeScope ideScope);
        void MonitorOpenProject(ProjectSettings settings, int? featureFileCount);
        void MonitorOpenFeatureFile(ProjectSettings projectSettings);

        void MonitorParserParse(ProjectSettings settings, Dictionary<string, string> additionalProps);

        void MonitorCommandCommentUncomment();
        void MonitorCommandDefineSteps(CreateStepDefinitionsDialogResult action, int snippetCount);
        void MonitorCommandFindStepDefinitionUsages(int usagesCount, bool isCancelled);
        void MonitorCommandGoToStepDefinition(bool generateSnippet);
        void MonitorCommandAutoFormatTable();
        void MonitorCommandAddFeatureFile(ProjectSettings projectSettings);

        void MonitorSpecFlowDiscovery(bool isFailed, string errorMessage, int stepDefinitionCount, ProjectSettings projectSettings);
        void MonitorSpecFlowGeneration(bool isFailed, ProjectSettings projectSettings);

        void MonitorError(Exception exception, bool? isFatal = null);

        void MonitorProjectTemplateWizardStarted();
        void MonitorProjectTemplateWizardCompleted(string dotNetFramework, string unitTestFramework, bool addFluentAssertions);
    }
}
