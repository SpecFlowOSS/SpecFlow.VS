using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using SpecFlow.VisualStudio.Analytics;
using SpecFlow.VisualStudio.Common;
using SpecFlow.VisualStudio.ProjectSystem;
using SpecFlow.VisualStudio.ProjectSystem.Settings;
using SpecFlow.VisualStudio.UI.ViewModels;

namespace SpecFlow.VisualStudio.Monitoring
{
    [Export(typeof(IMonitoringService))]
    public class MonitoringService : IMonitoringService
    {
        private readonly IAnalyticsTransmitter _analyticsTransmitter;
        private readonly IWelcomeService _welcomeService;

        [ImportingConstructor]
        public MonitoringService(IAnalyticsTransmitter analyticsTransmitter, IWelcomeService welcomeService, ITelemetryConfigurationHolder telemetryConfigurationHolder)
        {
            _analyticsTransmitter = analyticsTransmitter;
            _welcomeService = welcomeService;

            telemetryConfigurationHolder.ApplyConfiguration();
        }

        // OPEN

        public void MonitorLoadProjectSystem()
        {
            //currently we do nothing at this point
        }

        public void MonitorOpenProjectSystem(IIdeScope ideScope)
        {
            _welcomeService.OnIdeScopeActivityStarted(ideScope);

            //todo: add tfms
            _analyticsTransmitter.TransmitEvent(new GenericEvent("Extension loaded"));
        }

        public void MonitorOpenProject(ProjectSettings settings, int? featureFileCount)
        {
            _analyticsTransmitter.TransmitEvent(new GenericEvent("Project loaded",
                GetProjectSettingsProps(settings,
                    new Dictionary<string, object>()
                    {
                        { "FeatureFileCount", featureFileCount }
                    }
                    )));
        }

        public void MonitorOpenFeatureFile(ProjectSettings projectSettings)
        {
            _analyticsTransmitter.TransmitEvent(new GenericEvent("Feature file opened",
                GetProjectSettingsProps(projectSettings)));
        }

        public void MonitorParserParse(ProjectSettings settings, Dictionary<string, object> additionalProps)
        {
            _analyticsTransmitter.TransmitEvent(new GenericEvent("Feature file parsed", 
                GetProjectSettingsProps(settings, additionalProps)));
        }

        

        //COMMAND

        public void MonitorCommandCommentUncomment()
        {
            _analyticsTransmitter.TransmitEvent(new GenericEvent("CommentUncomment command executed"));
        }

        public void MonitorCommandDefineSteps(CreateStepDefinitionsDialogResult action, int snippetCount)
        {
            _analyticsTransmitter.TransmitEvent(new GenericEvent("DefineSteps command executed",
                new Dictionary<string, object>()
                {
                    { "Action", action },
                    { "SnippetCount", snippetCount }
                }));
        }

        public void MonitorCommandFindStepDefinitionUsages(int usagesCount, bool isCancelled)
        {
            _analyticsTransmitter.TransmitEvent(new GenericEvent("FindStepDefinitionUsages command executed",
                new Dictionary<string, object>()
                {
                    { "UsagesFound", usagesCount },
                    { "IsCancelled", isCancelled }
                }));
        }

        public void MonitorCommandGoToStepDefinition(bool generateSnippet)
        {
            _analyticsTransmitter.TransmitEvent(new GenericEvent("GoToStepDefinition command executed",
                new Dictionary<string, object>()
                {
                    { "GenerateSnippet", generateSnippet }
                }));
        }

        public void MonitorCommandAutoFormatTable()
        {
            //TODO: re-enable tracking for real command based triggering (not by character type)
            //nop
        }

        public void MonitorCommandAddFeatureFile(ProjectSettings settings)
        {
            _analyticsTransmitter.TransmitEvent(new GenericEvent("Feature file added",
                GetProjectSettingsProps(settings)));
        }

        public void MonitorCommandAddSpecFlowConfigFile()
        {
            _analyticsTransmitter.TransmitEvent(new GenericEvent("Specflow config added"));
        }


        //SPECFLOW

        public void MonitorSpecFlowDiscovery(bool isFailed, string errorMessage, int stepDefinitionCount, ProjectSettings projectSettings)
        {
            if (isFailed && !string.IsNullOrWhiteSpace(errorMessage))
            {
                var discoveryException = new DiscoveryException(errorMessage);
                _analyticsTransmitter.TransmitExceptionEvent(discoveryException, GetProjectSettingsProps(projectSettings));
            }

            var additionalProps = new Dictionary<string, object>()
            {
                { "IsFailed", isFailed },
                { "StepDefinitionCount", stepDefinitionCount }
            };
            _analyticsTransmitter.TransmitEvent(new GenericEvent("SpecFlow Discovery executed",
                GetProjectSettingsProps(projectSettings,
                    additionalProps)));
        }

        public void MonitorSpecFlowGeneration(bool isFailed, ProjectSettings projectSettings)
        {
            _analyticsTransmitter.TransmitEvent(new GenericEvent("SpecFlow Generation executed",
                GetProjectSettingsProps(projectSettings,
                    new Dictionary<string, object>()
                    {
                        { "IsFailed", isFailed }
                    })));
        }


        //ERROR

        public void MonitorError(Exception exception, bool? isFatal = null)
        {
            _analyticsTransmitter.TransmitExceptionEvent(exception, isFatal: isFatal);
        }


        // PROJECT TEMPLATE WIZARD

        public void MonitorProjectTemplateWizardStarted()
        {
            _analyticsTransmitter.TransmitEvent(new GenericEvent("Project Template Wizard Started"));
        }

        public void MonitorProjectTemplateWizardCompleted(string dotNetFramework, string unitTestFramework, bool addFluentAssertions)
        {
            _analyticsTransmitter.TransmitEvent(new GenericEvent("Project Template Wizard Completed",
                new Dictionary<string, object>()
                {
                    { "SelectedDotNetFramework", dotNetFramework },
                    { "SelectedUnitTestFramework", unitTestFramework },
                    { "AddFluentAssertions", addFluentAssertions },
                }));
        }

        public void MonitorCommandAutoFormatDocument(bool isSelectionFormatting)
        {
            _analyticsTransmitter.TransmitEvent(new GenericEvent("AutoFormatDocument command executed",
                new Dictionary<string, object>()
                    {
                        { "IsSelectionFormatting", isSelectionFormatting }
                    }));
        }


        private Dictionary<string, object> GetProjectSettingsProps(ProjectSettings settings, Dictionary<string, object> additionalSettings = null)
        {
            Dictionary<string, object> props = null;
            if (settings != null)
            {
                props = new Dictionary<string, object>
                {
                    { "SpecFlowVersion", settings.GetSpecFlowVersionLabel() },
                    //todo: add TFM(s) to the events
                    //{ "net", settings.TargetFrameworkMoniker.ToShortString() },
                    { "SingleFileGeneratorUsed", settings.DesignTimeFeatureFileGenerationEnabled },
                };
            }
            if (additionalSettings != null && additionalSettings.Any())
            {
                props ??= new Dictionary<string, object>();
                foreach (var additionalSetting in additionalSettings)
                {
                    props.Add(additionalSetting.Key, additionalSetting.Value);
                }
            }
            return props;
        }
    }
}
