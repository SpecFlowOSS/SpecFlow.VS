using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO.Abstractions;
using System.Linq;
using System.Windows.Threading;
using SpecFlow.VisualStudio.Analytics;

namespace SpecFlow.VisualStudio.ProjectSystem
{
    public interface IWelcomeService
    {
        void OnIdeScopeActivityStarted(IIdeScope ideScope);
    }

    [Export(typeof(IWelcomeService))]
    public class WelcomeService : IWelcomeService
    {
        private readonly IRegistryManager _registryManager;
        private readonly IVersionProvider _versionProvider;
        private readonly IAnalyticsTransmitter _analyticsTransmitter;
        private readonly IGuidanceConfiguration _guidanceConfiguration;
        private readonly IFileSystem _fileSystem;
        
        private static DispatcherTimer _welcomeMessageTimer;
        private static string _deveroomNews = null;

        [ImportingConstructor]
        public WelcomeService(IRegistryManager registryManager, IVersionProvider versionProvider, IAnalyticsTransmitter analyticsTransmitter, IGuidanceConfiguration guidanceConfiguration, IFileSystem fileSystem)
        {
            _registryManager = registryManager;
            _versionProvider = versionProvider;
            _analyticsTransmitter = analyticsTransmitter;
            _guidanceConfiguration = guidanceConfiguration;
            _fileSystem = fileSystem;
        }

        public void OnIdeScopeActivityStarted(IIdeScope ideScope)
        {
            UpdateUsageOfExtension(ideScope);
            
            //TODO: implement welcome dialog changes

            //    if (ActivityTracker.IsInstall)
            //    {
            //        ScheduleWelcomeDialog(ideScope, new WelcomeDialogViewModel(),
            //            (viewModel, elapsed) =>
            //            {
            //                EventTracker.TrackWelcomeInstall((int) elapsed.TotalSeconds, viewModel.VisitedPages.Count);
            //            });
            //    }
            //    else if (ActivityTracker.IsUpgrade)
            //    {
            //        StartDownloadNews();
            //        var selectedChangelog = GetSelectedChangelog(ideScope);
            //        ScheduleWelcomeDialog(ideScope, new WhatsNewDialogViewModel(ActivityTracker.AppVersion, selectedChangelog),
            //            (viewModel, elapsed) =>
            //            {
            //                EventTracker.TrackWelcomeUpgrade(
            //                    ActivityTracker.LastInstalledVersion ?? "na", ActivityTracker.AppVersion,
            //                    (int)elapsed.TotalSeconds, viewModel.VisitedPages.Count);
            //            },
            //            viewModel =>
            //            {
            //                var newsPage = viewModel.OtherNewsPage;
            //                if (_deveroomNews != null && newsPage != null)
            //                {
            //                    EventTracker.TrackWelcomeNewsLoaded(_deveroomNews);
            //                    newsPage.Text = WhatsNewDialogViewModel.ACTUAL_INFO_HEADER + _deveroomNews;
            //                }
            //            });
            //    }
        }

        private void UpdateUsageOfExtension(IIdeScope ideScope)
        {
            var today = DateTime.Today;
            var status = _registryManager.GetInstallStatus();
            var currentVersion = new Version(_versionProvider.GetExtensionVersion());

            if (!status.IsInstalled)
            {
                // new user
                // todo: missing implementation of browser notification
                //if (ShowNotification(_guidanceConfiguration.Installation))
                //{
                _analyticsTransmitter.TransmitEvent(new GenericEvent("Extension installed"));

                status.InstallDate = today;
                status.InstalledVersion = currentVersion;
                status.LastUsedDate = today;

                _registryManager.UpdateStatus(status);
                CheckFileAssociation(ideScope);
                //}
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
                        _registryManager.UpdateStatus(status);
                        //}
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

        //private bool ShowNotification(GuidanceStep guidance)
        //{
        //    var url = guidance.Url;

        //    return notificationService.ShowPage(url);
        //}

        //private static void StartDownloadNews()
        //{
        //    try
        //    {
        //        var request = (HttpWebRequest)WebRequest.Create("https://www.specsolutions.eu/media/deveroom/deveroom_news.md");
        //        request.Method = "GET";
        //        request.GetResponseAsync().ContinueWith(ReadNews, TaskScheduler.Default);
        //    }
        //    catch (Exception ex)
        //    {
        //        EventTracker.TrackError(ex);
        //    }
        //}

        //private static void ReadNews(Task<WebResponse> response)
        //{
        //    try
        //    {
        //        if (response.Status == TaskStatus.RanToCompletion &&
        //            response.Result is HttpWebResponse httpWebResponse &&
        //            httpWebResponse.StatusCode == HttpStatusCode.OK)
        //        {
        //            var responseStream = httpWebResponse.GetResponseStream();
        //            if (responseStream != null)
        //            {
        //                var reader = new StreamReader(responseStream);
        //                var result = reader.ReadToEnd();
        //                _deveroomNews = result;
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        EventTracker.TrackError(ex);
        //    }
        //}

        //private static string GetSelectedChangelog(IIdeScope ideScope)
        //{
        //    string changeLog = GetChangeLog(ideScope);

        //    int start = 0;
        //    var newVersionMatch = Regex.Match(changeLog, @"^# v" + ActivityTracker.AppVersion, RegexOptions.Multiline);
        //    if (newVersionMatch.Success)
        //        start = newVersionMatch.Index;

        //    int end = changeLog.Length;
        //    var lastInstalledVersion = ActivityTracker.LastInstalledVersion;
        //    if (lastInstalledVersion != null)
        //    {
        //        var oldVersionMatch = Regex.Match(changeLog, @"^# v" + lastInstalledVersion, RegexOptions.Multiline);
        //        if (oldVersionMatch.Success)
        //            end = oldVersionMatch.Index;
        //    }

        //    var selectedChangelog = changeLog.Substring(start, end - start);
        //    return selectedChangelog;
        //}

        //private static string GetChangeLog(IIdeScope ideScope)
        //{
        //    try
        //    {
        //        var extensionFolder = ideScope.GetExtensionFolder();
        //        var changeLogPath = Path.Combine(extensionFolder, "Changelog.txt");
        //        if (!ideScope.FileSystem.File.Exists(changeLogPath))
        //            return string.Empty;
        //        return ideScope.FileSystem.File.ReadAllText(changeLogPath);
        //    }
        //    catch (Exception ex)
        //    {
        //        EventTracker.TrackError(ex);
        //        return string.Empty;
        //    }
        //}

        //private static void ScheduleWelcomeDialog<TViewModel>(IIdeScope ideScope, TViewModel viewModel, Action<TViewModel, TimeSpan> onClosed, Action<TViewModel> onOpen = null)
        //{
        //    _welcomeMessageTimer = new DispatcherTimer(DispatcherPriority.ApplicationIdle)
        //    {
        //        Interval = TimeSpan.FromSeconds(7)
        //    };
        //    _welcomeMessageTimer.Tick += (sender, args) =>
        //    {
        //        _welcomeMessageTimer.Stop();
        //        _welcomeMessageTimer = null;
        //        onOpen?.Invoke(viewModel);
        //        var stopwatch = new Stopwatch();
        //        stopwatch.Start();
        //        ideScope.WindowManager.ShowDialog(viewModel);
        //        stopwatch.Stop();
        //        onClosed?.Invoke(viewModel, stopwatch.Elapsed);
        //    };
        //    _welcomeMessageTimer.Start();
        //}
    }
}
