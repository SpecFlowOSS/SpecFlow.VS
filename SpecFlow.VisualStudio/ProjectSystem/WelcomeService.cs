using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Threading;
using SpecFlow.VisualStudio.EventTracking;
using SpecFlow.VisualStudio.UI.ViewModels;

namespace SpecFlow.VisualStudio.ProjectSystem
{
    public static class WelcomeService
    {
        private static DispatcherTimer _welcomeMessageTimer;
        private static string _deveroomNews = null;

        public static void OnIdeScopeActivityStarted(IIdeScope ideScope)
        {
            if (ActivityTracker.IsInstall)
            {
                ScheduleWelcomeDialog(ideScope, new WelcomeDialogViewModel(),
                    (viewModel, elapsed) =>
                    {
                        EventTracker.TrackWelcomeInstall((int) elapsed.TotalSeconds, viewModel.VisitedPages.Count);
                    });
            }
            else if (ActivityTracker.IsUpgrade)
            {
                StartDownloadNews();
                var selectedChangelog = GetSelectedChangelog(ideScope);
                ScheduleWelcomeDialog(ideScope, new WhatsNewDialogViewModel(ActivityTracker.AppVersion, selectedChangelog),
                    (viewModel, elapsed) =>
                    {
                        EventTracker.TrackWelcomeUpgrade(
                            ActivityTracker.LastInstalledVersion ?? "na", ActivityTracker.AppVersion,
                            (int)elapsed.TotalSeconds, viewModel.VisitedPages.Count);
                    },
                    viewModel =>
                    {
                        var newsPage = viewModel.OtherNewsPage;
                        if (_deveroomNews != null && newsPage != null)
                        {
                            EventTracker.TrackWelcomeNewsLoaded(_deveroomNews);
                            newsPage.Text = WhatsNewDialogViewModel.ACTUAL_INFO_HEADER + _deveroomNews;
                        }
                    });
            }
        }

        private static void StartDownloadNews()
        {
            try
            {
                var request = (HttpWebRequest)WebRequest.Create("https://www.specsolutions.eu/media/deveroom/deveroom_news.md");
                request.Method = "GET";
                request.GetResponseAsync().ContinueWith(ReadNews, TaskScheduler.Default);
            }
            catch (Exception ex)
            {
                EventTracker.TrackError(ex);
            }
        }

        private static void ReadNews(Task<WebResponse> response)
        {
            try
            {
                if (response.Status == TaskStatus.RanToCompletion &&
                    response.Result is HttpWebResponse httpWebResponse &&
                    httpWebResponse.StatusCode == HttpStatusCode.OK)
                {
                    var responseStream = httpWebResponse.GetResponseStream();
                    if (responseStream != null)
                    {
                        var reader = new StreamReader(responseStream);
                        var result = reader.ReadToEnd();
                        _deveroomNews = result;
                    }
                }
            }
            catch (Exception ex)
            {
                EventTracker.TrackError(ex);
            }
        }

        private static string GetSelectedChangelog(IIdeScope ideScope)
        {
            string changeLog = GetChangeLog(ideScope);

            int start = 0;
            var newVersionMatch = Regex.Match(changeLog, @"^# v" + ActivityTracker.AppVersion, RegexOptions.Multiline);
            if (newVersionMatch.Success)
                start = newVersionMatch.Index;

            int end = changeLog.Length;
            var lastInstalledVersion = ActivityTracker.LastInstalledVersion;
            if (lastInstalledVersion != null)
            {
                var oldVersionMatch = Regex.Match(changeLog, @"^# v" + lastInstalledVersion, RegexOptions.Multiline);
                if (oldVersionMatch.Success)
                    end = oldVersionMatch.Index;
            }

            var selectedChangelog = changeLog.Substring(start, end - start);
            return selectedChangelog;
        }

        private static string GetChangeLog(IIdeScope ideScope)
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
                EventTracker.TrackError(ex);
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
