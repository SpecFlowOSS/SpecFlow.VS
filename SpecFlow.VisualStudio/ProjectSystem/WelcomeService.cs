using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Threading;
using Microsoft.VisualStudio.Shell;
using SpecFlow.VisualStudio.Analytics;
using SpecFlow.VisualStudio.Diagnostics;
using SpecFlow.VisualStudio.Notifications;
using SpecFlow.VisualStudio.UI.ViewModels;

namespace SpecFlow.VisualStudio.ProjectSystem
{
    public interface IWelcomeService
    {
        void OnIdeScopeActivityStarted(IIdeScope ideScope);
    }

    [Export(typeof(IWelcomeService))]
    public class WelcomeService : IWelcomeService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IRegistryManager _registryManager;
        private readonly IVersionProvider _versionProvider;
        private readonly IAnalyticsTransmitter _analyticsTransmitter;
        private readonly IGuidanceConfiguration _guidanceConfiguration;
        private readonly IFileSystem _fileSystem;

        private NotificationService _notificationService;

        private static DispatcherTimer _welcomeMessageTimer;
        private static string _deveroomNews = null;

        [ImportingConstructor]
        public WelcomeService([Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider, IRegistryManager registryManager, IVersionProvider versionProvider, IAnalyticsTransmitter analyticsTransmitter, IGuidanceConfiguration guidanceConfiguration, IFileSystem fileSystem)
        {
            _serviceProvider = serviceProvider;
            _registryManager = registryManager;
            _versionProvider = versionProvider;
            _analyticsTransmitter = analyticsTransmitter;
            _guidanceConfiguration = guidanceConfiguration;
            _fileSystem = fileSystem;
        }

        public void OnIdeScopeActivityStarted(IIdeScope ideScope)
        {
            UpdateUsageOfExtension(ideScope);

            var notificationDataStore = new NotificationDataStore();
            _notificationService = new NotificationService(notificationDataStore,
                    new NotificationInfoBarFactory(_serviceProvider, ideScope, notificationDataStore, _analyticsTransmitter));
            _notificationService.Initialize();
        }

        private void UpdateUsageOfExtension(IIdeScope ideScope)
        {
            var today = DateTime.Today;
            var status = _registryManager.GetInstallStatus();
            var currentVersion = new Version(_versionProvider.GetExtensionVersion());
            var browserNotificationService = new ExternalBrowserNotificationService(ideScope);

            if (!status.IsInstalled)
            {
                // new user
                browserNotificationService.ShowPage(_guidanceConfiguration.Installation.Url);
                _analyticsTransmitter.TransmitEvent(new GenericEvent("Extension installed"));

                status.InstallDate = today;
                status.InstalledVersion = currentVersion;
                status.LastUsedDate = today;

                _registryManager.UpdateStatus(status);
                CheckFileAssociation(ideScope);
            }
            else
            {
                if (status.LastUsedDate != today)
                {
                    //a shiny new day with SpecFlow
                    status.UsageDays++;
                    status.LastUsedDate = today;
                    _registryManager.UpdateStatus(status);
                }

                if (status.InstalledVersion < currentVersion)
                {
                    //upgrading user
                    var installedVersion = status.InstalledVersion.ToString();
                    _analyticsTransmitter.TransmitEvent(new GenericEvent("Extension upgraded",
                        new Dictionary<string, object>
                        {
                            { "OldExtensionVersion", installedVersion }
                        }));

                    status.InstallDate = today;
                    status.InstalledVersion = currentVersion;

                    _registryManager.UpdateStatus(status);
                    CheckFileAssociation(ideScope);

                    var selectedChangelog = GetSelectedChangelog(ideScope, installedVersion);
                    ScheduleWelcomeDialog(ideScope, new WhatsNewDialogViewModel(currentVersion.ToString(), selectedChangelog),
                        (viewModel, elapsed) =>
                        {
                            _analyticsTransmitter.TransmitEvent(new GenericEvent("Upgrade dialog dismissed",
                                new Dictionary<string, object>
                                {
                                    { "OldExtensionVersion", installedVersion },
                                    { "NewExtensionVersion", currentVersion },
                                    { "WelcomeScreenSeconds", (int)elapsed.TotalSeconds },
                                    { "VisitedPages", viewModel.VisitedPages.Count },
                                }));
                        },
                        viewModel =>
                        {
                            //todo: implement second page
                            //var newsPage = viewModel.OtherNewsPage;
                            //if (_deveroomNews != null && newsPage != null)
                            //{
                            //    //EventTracker.TrackWelcomeNewsLoaded(_deveroomNews);
                            //    newsPage.Text = WhatsNewDialogViewModel.ACTUAL_INFO_HEADER + _deveroomNews;
                            //}
                        });
                }
                else
                {
                    var guidance = _guidanceConfiguration.UsageSequence
                        .FirstOrDefault(i => status.UsageDays >= i.UsageDays && status.UserLevel < (int)i.UserLevel);

                    if (guidance?.UsageDays != null)
                    {
                        var usageDays = guidance.UsageDays.Value;
                        _analyticsTransmitter.TransmitEvent(new GenericEvent($"{usageDays} day usage"));

                        status.UserLevel = (int)guidance.UserLevel;
                        _registryManager.UpdateStatus(status);
                    }
                }
            }
        }

        private void CheckFileAssociation(IIdeScope ideScope)
        {
            var associationDetector = new WindowsFileAssociationDetector(_fileSystem, ideScope);
            var isAssociated = associationDetector.IsAssociated();
            if (isAssociated != null && !isAssociated.Value)
            {
                associationDetector.SetAssociation();
            }
        }

        private string GetSelectedChangelog(IIdeScope ideScope, string currentExtensionVersion)
        {
            string changeLog = GetChangeLog(ideScope);

            int start = 0;
            var newVersionMatch = Regex.Match(changeLog, @"^# v" + _versionProvider.GetExtensionVersion(), RegexOptions.Multiline);
            if (newVersionMatch.Success)
                start = newVersionMatch.Index;

            int end = changeLog.Length;
            if (currentExtensionVersion != null)
            {
                var oldVersionMatch = Regex.Match(changeLog, @"^# v" + currentExtensionVersion, RegexOptions.Multiline);
                if (oldVersionMatch.Success)
                    end = oldVersionMatch.Index;
            }

            var selectedChangelog = changeLog.Substring(start, end - start);
            return selectedChangelog;
        }

        private string GetChangeLog(IIdeScope ideScope)
        {
            try
            {
                var extensionFolder = ideScope.GetExtensionFolder();
                var changeLogPath = Path.Combine(extensionFolder, "Changelog.txt");
                if (!ideScope.FileSystem.File.Exists(changeLogPath))
                    return string.Empty;
                return ideScope.FileSystem.File.ReadAllText(changeLogPath);
            }
            catch (Exception ex)
            {
                ideScope.Logger.LogException(ideScope.MonitoringService, ex);
                return string.Empty;
            }
        }

        private static void ScheduleWelcomeDialog<TViewModel>(IIdeScope ideScope, TViewModel viewModel, Action<TViewModel, TimeSpan> onClosed, Action<TViewModel> onOpen = null)
        {
            _welcomeMessageTimer = new DispatcherTimer(DispatcherPriority.ApplicationIdle)
            {
                Interval = TimeSpan.FromSeconds(7)
            };
            _welcomeMessageTimer.Tick += (sender, args) =>
            {
                _welcomeMessageTimer.Stop();
                _welcomeMessageTimer = null;
                onOpen?.Invoke(viewModel);
                var stopwatch = new Stopwatch();
                stopwatch.Start();
                ideScope.WindowManager.ShowDialog(viewModel);
                stopwatch.Stop();
                onClosed?.Invoke(viewModel, stopwatch.Elapsed);
            };
            _welcomeMessageTimer.Start();
        }
    }
}
