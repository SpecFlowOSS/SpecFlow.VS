using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Deveroom.VisualStudio.Common;
using Deveroom.VisualStudio.ProjectSystem.Settings;
using Deveroom.VisualStudio.UI.ViewModels;

namespace Deveroom.VisualStudio.EventTracking
{
    public static class EventTracker
    {
        internal static string HostId { get; private set; } = "unknown";

        private static bool _isEnabled = true;
        private static readonly Lazy<IAnalyticsApi> _analyticsApi = new Lazy<IAnalyticsApi>(InitializeApi, LazyThreadSafetyMode.ExecutionAndPublication);

        private static IAnalyticsApi InitializeApi()
        {
            try
            {
                IAnalyticsApi api = new AnalyticsApi();
                if (!api.HasClientId)
                    api.TrackEvent("init", "newcid", "");

                if (api.SenderError != null)
                {
                    var senderErrorMessage = ErrorAnonymizer.AnonymizeException(api.SenderError);
                    api.TrackException(senderErrorMessage, false);
                }

                return api;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex, "Deveroom:GA");
                return null;
            }
        }

        static EventTracker()
        {
            var processName = Process.GetCurrentProcess().ProcessName;
            if (!processName.Equals("devenv", StringComparison.InvariantCultureIgnoreCase))
                _isEnabled = false;
        }

        public static void SetVsVersion(string vsVersion)
        {
            HostId = "VS" + vsVersion;
        }

        private static void TrackEvent(string category, string action, string label = "", int? value = null)
        {
            if (_isEnabled)
            {
                _analyticsApi.Value?.TrackEvent(category, action, label, value);
            }
        }

        private static void TrackException(string exceptionDetail, bool isFatal)
        {
            if (_isEnabled)
            {
                if (_analyticsApi.Value?.LogEnabled ?? false)
                {
                    _analyticsApi.Value?.Log(exceptionDetail);
                    _analyticsApi.Value?.Log(exceptionDetail.Substring(0, Math.Min(150, exceptionDetail.Length)));
                }
                _analyticsApi.Value?.TrackException(exceptionDetail, isFatal);
            }
        }

        private static string GetLabel(Dictionary<string, object> details)
        {
            return
                string.Join(",",
                    details.Where(d => !(d.Value is bool) || (bool) d.Value)
                        .Select(d => d.Value is bool ? d.Key : $"{d.Key}={d.Value}"));
        }

        public static void TrackOpenFeatureFile(ProjectSettings settings)
        {
            TrackEvent("open", "ff", GetLabelForProjectSettings(settings));
        }

        public static void TrackOpenProject(ProjectSettings settings, int? featureFileCount)
        {
            TrackEvent("open", "prj", GetLabelForProjectSettings(settings), featureFileCount);
        }

        private static string GetLabelForProjectSettings(ProjectSettings settings, params KeyValuePair<string, object>[] additionalSettings)
        {
            var dictionary = settings != null ? new Dictionary<string, object>
            {
                {"sf", settings.GetSpecFlowVersionLabel() },
                {"net", settings.TargetFrameworkMoniker.ToShortString() },
                {"g", settings.DesignTimeFeatureFileGenerationEnabled },
            } : new Dictionary<string, object>
            {
                {"noprj", true }
            };
            if (additionalSettings != null && additionalSettings.Length > 0)
                foreach (var additionalSetting in additionalSettings)
                {
                    dictionary.Add(additionalSetting.Key, additionalSetting.Value);
                }
            return GetLabel(dictionary);
        }

        public static void TrackOpenProjectSystem(string vsVersion, int activeDays)
        {
            SetVsVersion(vsVersion);
            TrackEvent("open", "psys", vsVersion, activeDays);
        }

        public static void TrackCommandDefineSteps(CreateStepDefinitionsDialogResult action, int snippetCount)
        {
            TrackEvent("comm", "defsteps", action.ToString(), snippetCount);
        }

        public static void TrackCommandCommentUncomment()
        {
            TrackEvent("comm", "comment");
        }

        public static void TrackCommandAutoFormatTable()
        {
            TrackEvent("comm", "formtabl");
        }

        public static void TrackCommandGoToStepDefinition(bool generateSnippet)
        {
            TrackEvent("comm", "gotodef", GetLabel(new Dictionary<string, object>
            {
                {"defstep", generateSnippet },
            }));
        }

        public static void TrackCommandFindStepDefinitionUsages(int usagesFound, bool isCancelled)
        {
            TrackEvent("comm", "findsteps", GetLabel(new Dictionary<string, object>
            {
                {"cancelled", isCancelled }
            }), usagesFound);
        }

        public static void TrackCommandAddFeatureFile(ProjectSettings settings)
        {
            TrackEvent("comm", "addff", GetLabelForProjectSettings(settings));
        }


        public static void TrackSpecFlowDiscovery(bool isFailed, int stepDefinitionsCount, ProjectSettings settings)
        {
            TrackEvent("sf", "discovery", GetLabelForProjectSettings(settings, new KeyValuePair<string, object>("failed", isFailed)), stepDefinitionsCount);
        }

        public static void TrackSpecFlowGeneration(bool isFailed, ProjectSettings settings)
        {
            TrackEvent("sf", "gen", GetLabelForProjectSettings(settings, new KeyValuePair<string, object>("failed", isFailed)));
        }



        public static void TrackWelcomeInstall(int welcomeScreenSeconds, int visitedPagesCount)
        {
            TrackEvent("welc", "install", GetLabel(new Dictionary<string, object>
            {
                {"p", visitedPagesCount },
            }), Math.Max(welcomeScreenSeconds, 60));
        }

        public static void TrackWelcomeUpgrade(string from, string to, int welcomeScreenSeconds, int visitedPagesCount)
        {
            TrackEvent("welc", "upgrade", GetLabel(new Dictionary<string, object>
            {
                {"fv", from },
                {"tv", to },
                {"p", visitedPagesCount },
            }), Math.Max(welcomeScreenSeconds, 60));
        }

        public static void TrackWelcomeLinkClick(string link)
        {
            TrackEvent("welc", "link", link);
        }

        public static void TrackWelcomeNewsLoaded(string news)
        {
            var firstLine = news?.Split(new []{'\r', '\n'}, 2)[0].Trim('#', ' ');
            TrackEvent("welc", "newsloaded", firstLine);
        }



        public static void TrackParserParse(int parseCount, int scenarioDefinitionCount)
        {
            TrackEvent("parser", "parse", GetLabel(new Dictionary<string, object>
            {
                {"c", parseCount },
            }), scenarioDefinitionCount);
        }


        public static void TrackError(Exception exception, bool? isFatal = null)
        {
            var isNormalError = IsNormalError(exception);
            var anonymMessage = ErrorAnonymizer.AnonymizeException(exception);
            TrackException(anonymMessage, isFatal ?? !isNormalError);
        }

        public static void TrackError(string errorMessage, ProjectSettings settings, bool isFatal = false)
        {
            var label = GetLabelForProjectSettings(settings);
            TrackError(errorMessage, label, isFatal);
        }

        public static void TrackError(string errorMessage, string label = null, bool? isFatal = null, bool anonymize = true)
        {
            if (anonymize)
                errorMessage = ErrorAnonymizer.AnonymizeErrorMessage(errorMessage, 150 - (label?.Length - 1) ?? 0);
            var errorDetails = label == null ? errorMessage : $"{label}:{errorMessage}";
            TrackException(errorDetails, isFatal ?? false);
        }

        public static bool IsNormalError(Exception exception)
        {
            if (exception is AggregateException aggregateException)
                return aggregateException.InnerExceptions.All(IsNormalError);
            return 
                exception is DeveroomConfigurationException ||
                exception is TimeoutException ||
                exception is TaskCanceledException ||
                exception is OperationCanceledException ||
                exception is HttpRequestException;
        }

        public static string GetExceptionMessage(Exception ex)
        {
            var message = ex.Message;
            while (ex.InnerException != null)
            {
                ex = ex.InnerException;
                message += "->" + ex.Message;
            }

            return message;
        }

        public static void Disable()
        {
            _isEnabled = false;
        }
    }
}
