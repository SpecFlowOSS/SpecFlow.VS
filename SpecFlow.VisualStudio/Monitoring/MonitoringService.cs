using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.VisualStudio.ApplicationInsights.Channel;
using Microsoft.VisualStudio.ApplicationInsights.Extensibility;
using SpecFlow.VisualStudio.Analytics;
using SpecFlow.VisualStudio.Common;
using SpecFlow.VisualStudio.EventTracking;
using SpecFlow.VisualStudio.ProjectSystem;
using SpecFlow.VisualStudio.ProjectSystem.Settings;
using SpecFlow.VisualStudio.UI.ViewModels;

namespace SpecFlow.VisualStudio.Monitoring
{
    [Export(typeof(IMonitoringService))]
    public class MonitoringService : IMonitoringService
    {
        private readonly IAnalyticsTransmitter _analyticsTransmitter;
        private readonly IStatusAccessor _statusAccessor;
        private readonly IGuidanceConfiguration _guidanceConfiguration;

        [ImportingConstructor]
        public MonitoringService(IAnalyticsTransmitter analyticsTransmitter, IStatusAccessor statusAccessor, IGuidanceConfiguration guidanceConfiguration, IContextInitializer contextInitializer)
        {
            TelemetryConfiguration.Active.InstrumentationKey = "27cfb992-6c29-4bc8-8093-78d95e275b3a";
            TelemetryConfiguration.Active.TelemetryChannel = new InMemoryChannel();
            TelemetryConfiguration.Active.ContextInitializers.Add(contextInitializer);

            _analyticsTransmitter = analyticsTransmitter;
            _statusAccessor = statusAccessor;
            _guidanceConfiguration = guidanceConfiguration;
        }

        // OPEN

        public void MonitorLoadProjectSystem()
        {
            //currently we do nothing at this point
        }

        public void MonitorOpenProjectSystem(IIdeScope ideScope)
        {
            UpdateUsageOfExtension();

            //todo: add tfms
            _analyticsTransmitter.TransmitEvent(new GenericEvent("Extension loaded"));
        }

        public void MonitorOpenProject(ProjectSettings settings, int? featureFileCount)
        {
            _analyticsTransmitter.TransmitEvent(new GenericEvent("Project loaded",
                GetProjectSettingsProps(settings,
                    new Dictionary<string, string>()
                    {
                        { "FeatureFileCount", featureFileCount.ToString() }
                    }
                    )));
        }

        public void MonitorOpenFeatureFile(ProjectSettings projectSettings)
        {
            _analyticsTransmitter.TransmitEvent(new GenericEvent("Feature file opened",
                GetProjectSettingsProps(projectSettings)));
        }

        public void MonitorParserParse(ProjectSettings settings, int parseCount, int scenarioDefinitionCount)
        {
            _analyticsTransmitter.TransmitEvent(new GenericEvent("Feature file parsed", 
                GetProjectSettingsProps(settings,
                    new Dictionary<string, string>()
                    {
                        { "ParseCount", parseCount.ToString() },
                        { "ScenarioDefinitionsCount", scenarioDefinitionCount.ToString() }
                    })));
            EventTracker.TrackParserParse(parseCount, scenarioDefinitionCount);
        }

        private void UpdateUsageOfExtension()
        {
            var today = DateTime.Today;
            var status = _statusAccessor.GetInstallStatus();
            //todo: discuss extension versioning
            var CurrentVersion = new Version(1, 0);

            if (!status.IsInstalled)
            {
                // new user
                // todo: missing implementation of browser notification
                //if (ShowNotification(_guidanceConfiguration.Installation))
                //{
                _analyticsTransmitter.TransmitEvent(new GenericEvent("Extension installed"));

                status.InstallDate = today;
                status.InstalledVersion = CurrentVersion;
                status.LastUsedDate = today;

                _statusAccessor.UpdateStatus(status);
                //todo
                //CheckFileAssociation();
                //}
            }
            else
            {
                if (status.LastUsedDate != today)
                {
                    //a shiny new day with SpecFlow
                    status.UsageDays++;
                    status.LastUsedDate = today;
                    _statusAccessor.UpdateStatus(status);
                }

                if (status.InstalledVersion < CurrentVersion)
                {
                    //upgrading user
                    var installedVersion = status.InstalledVersion.ToString();
                    _analyticsTransmitter.TransmitEvent(new GenericEvent("Extension upgraded",
                        new Dictionary<string, string>
                        {
                            { "OldExtensionVersion", installedVersion }
                        }));

                    status.InstallDate = today;
                    status.InstalledVersion = CurrentVersion;

                    _statusAccessor.UpdateStatus(status);
                }
                else
                {
                    var guidance = _guidanceConfiguration.UsageSequence
                        .FirstOrDefault(i => status.UsageDays >= i.UsageDays && status.UserLevel < (int)i.UserLevel);

                    if (guidance?.UsageDays != null)
                    {
                        // todo: missing implementation of browser notification
                        //if (guidance.Url == null || ShowNotification(guidance))
                        //{
                        var usageDays = guidance.UsageDays.Value;
                        _analyticsTransmitter.TransmitEvent(new GenericEvent($"{usageDays} day usage"));

                        status.UserLevel = (int)guidance.UserLevel;
                        _statusAccessor.UpdateStatus(status);
                        //}
                    }
                }
            }
        }

        //private bool ShowNotification(GuidanceStep guidance)
        //{
        //    var url = guidance.Url;

        //    return notificationService.ShowPage(url);
        //}

        //COMMAND

        public void MonitorCommandCommentUncomment()
        {
            _analyticsTransmitter.TransmitEvent(new GenericEvent("CommentUncomment command executed"));
        }

        public void MonitorCommandDefineSteps(CreateStepDefinitionsDialogResult action, int snippetCount)
        {
            _analyticsTransmitter.TransmitEvent(new GenericEvent("DefineSteps command executed",
                new Dictionary<string, string>()
                {
                    { "Action", action.ToString() },
                    { "SnippetCount", snippetCount.ToString() }
                }));
        }

        public void MonitorCommandFindStepDefinitionUsages(int usagesCount, bool isCancelled)
        {
            _analyticsTransmitter.TransmitEvent(new GenericEvent("FindStepDefinitionUsages command executed",
                new Dictionary<string, string>()
                {
                    { "UsagesFound", usagesCount.ToString() },
                    { "IsCancelled", isCancelled.ToString() }
                }));
        }

        public void MonitorCommandGoToStepDefinition(bool generateSnippet)
        {
            _analyticsTransmitter.TransmitEvent(new GenericEvent("GoToStepDefinition command executed",
                new Dictionary<string, string>()
                {
                    { "GenerateSnippet", generateSnippet.ToString() }
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


        //SPECFLOW

        public void MonitorSpecFlowDiscovery(bool isFailed, string errorMessage, int stepDefinitionCount, ProjectSettings projectSettings)
        {
            if (isFailed && !string.IsNullOrWhiteSpace(errorMessage))
            {
                var discoveryException = new DiscoveryException(errorMessage);
                _analyticsTransmitter.TransmitExceptionEvent(discoveryException, GetProjectSettingsProps(projectSettings));
            }

            var additionalProps = new Dictionary<string, string>()
            {
                { "IsFailed", isFailed.ToString() },
                { "StepDefinitionCount", stepDefinitionCount.ToString() }
            };
            _analyticsTransmitter.TransmitEvent(new GenericEvent("SpecFlow Discovery executed",
                GetProjectSettingsProps(projectSettings,
                    additionalProps)));
        }

        public void MonitorSpecFlowGeneration(bool isFailed, ProjectSettings projectSettings)
        {
            _analyticsTransmitter.TransmitEvent(new GenericEvent("SpecFlow Discovery executed",
                GetProjectSettingsProps(projectSettings,
                    new Dictionary<string, string>()
                    {
                        { "IsFailed", isFailed.ToString() }
                    })));
        }


        //ERROR

        public void MonitorError(Exception exception, bool? isFatal = null)
        {
            _analyticsTransmitter.TransmitExceptionEvent(exception, isFatal);
        }

        private Dictionary<string, string> GetProjectSettingsProps(ProjectSettings settings, Dictionary<string, string> additionalSettings = null)
        {
            Dictionary<string, string> props = null;
            if (settings != null)
            {
                props = new Dictionary<string, string>
                {
                    { "SpecFlowVersion", settings.GetSpecFlowVersionLabel() },
                    //{ "net", settings.TargetFrameworkMoniker.ToShortString() },
                    { "SingleFileGeneratorUsed", settings.DesignTimeFeatureFileGenerationEnabled.ToString() },
                };
            }
            if (additionalSettings != null && additionalSettings.Any())
            {
                props ??= new Dictionary<string, string>();
                foreach (var additionalSetting in additionalSettings)
                {
                    props.Add(additionalSetting.Key, additionalSetting.Value);
                }
            }
            return props;
        }
    }
}
