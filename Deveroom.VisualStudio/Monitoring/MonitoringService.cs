using System;
using System.ComponentModel.Composition;
using Deveroom.VisualStudio.EventTracking;
using Deveroom.VisualStudio.ProjectSystem;
using Deveroom.VisualStudio.ProjectSystem.Settings;
using Deveroom.VisualStudio.UI.ViewModels;

namespace Deveroom.VisualStudio.Monitoring
{
    [Export(typeof(IMonitoringService))]
    public class MonitoringService : IMonitoringService
    {
        // OPEN

        public void MonitorLoadProjectSystem(string vsVersion)
        {
            EventTracker.SetVsVersion(vsVersion);
        }

        public void MonitorOpenProjectSystem(string vsVersion, IIdeScope ideScope)
        {
            EventTracker.TrackOpenProjectSystem(vsVersion, ActivityTracker.ActiveDays);
            //TODO:  WelcomeService.OnIdeScopeActivityStarted(ideScope);
        }

        public void MonitorOpenProject(ProjectSettings settings, int? featureFileCount)
        {
            EventTracker.TrackOpenProject(settings, featureFileCount);
        }

        public void MonitorOpenFeatureFile(ProjectSettings projectSettings)
        {
            EventTracker.TrackOpenFeatureFile(projectSettings);
        }

        public void MonitorParserParse(int parseCount, int scenarioDefinitionCount)
        {
            EventTracker.TrackParserParse(parseCount, scenarioDefinitionCount);
        }

        //COMMAND

        public void MonitorCommandCommentUncomment()
        {
            EventTracker.TrackCommandCommentUncomment();
        }

        public void MonitorCommandDefineSteps(CreateStepDefinitionsDialogResult action, int snippetCount)
        {
            EventTracker.TrackCommandDefineSteps(action, snippetCount);
        }

        public void MonitorCommandFindStepDefinitionUsages(int usagesCount, bool isCancelled)
        {
            EventTracker.TrackCommandFindStepDefinitionUsages(usagesCount, isCancelled);
        }

        public void MonitorCommandGoToStepDefinition(bool generateSnippet)
        {
            EventTracker.TrackCommandGoToStepDefinition(generateSnippet);
        }

        public void MonitorCommandAutoFormatTable()
        {
            //TODO: re-enable tracking for real command based triggering (not by character type)
            //nop
        }

        public void MonitorCommandAddFeatureFile(ProjectSettings projectSettings)
        {
            EventTracker.TrackCommandAddFeatureFile(projectSettings);
        }


        //SPECFLOW

        public void MonitorSpecFlowDiscovery(bool isFailed, string errorMessage, int stepDefinitionCount, ProjectSettings projectSettings)
        {
            if (isFailed && !string.IsNullOrWhiteSpace(errorMessage))
            {
                EventTracker.TrackError(errorMessage, projectSettings);
            }

            EventTracker.TrackSpecFlowDiscovery(isFailed, stepDefinitionCount, projectSettings);
        }

        public void MonitorSpecFlowGeneration(bool isFailed, ProjectSettings projectSettings)
        {
            EventTracker.TrackSpecFlowGeneration(isFailed, projectSettings);
        }


        //ERROR

        public void MonitorError(Exception exception, bool? isFatal = null)
        {
            if (exception is InvalidOperationException && exception.StackTrace.Contains("MatchToken"))
            {
                // gather extra information about this error
                EventTracker.TrackError($"MT:{exception.GetFlattenedMessage()}", anonymize: false);
            }
            EventTracker.TrackError(exception, isFatal);
        }
    }
}
