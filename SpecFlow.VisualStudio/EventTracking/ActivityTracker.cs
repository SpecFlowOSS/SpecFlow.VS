using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SpecFlow.VisualStudio.EventTracking
{
    public static class ActivityTracker
    {
        private static readonly DateTime MagicDate = new DateTime(2009, 9, 10);
        public static readonly string AppVersion = GetAppVersion();
        private static readonly Lazy<TrackerData> _trackerData = new Lazy<TrackerData>(LoadTrackerData, true);

        public static bool IsInstall => _trackerData.Value?.IsInstall ?? false;
        public static bool IsUpgrade => _trackerData.Value?.IsUpgrade ?? false;
        public static string LastInstalledVersion => _trackerData.Value?.VersionInstalls
            .OrderByDescending(kv => kv.Value).Select(kv => kv.Key).FirstOrDefault()?.TrimStart('v');
        public static int ActiveDays => _trackerData.Value?.ActiveDays ?? 0;

        private static string GetAppVersion()
        {
            var assembly = Assembly.GetAssembly(typeof(ActivityTracker));
            var versionAttr = assembly.GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute)).OfType<AssemblyInformationalVersionAttribute>().FirstOrDefault();
            if (versionAttr != null)
            {
                return versionAttr.InformationalVersion.Split('+', '-')[0];
            }
            return assembly.GetName().Version.ToString(3);
        }

        class TrackerData
        {
            public Dictionary<string, DateTime> VersionInstalls { get; set; }
            public int ActiveDays { get; set; }
            public DateTime? LastActivity { get; set; }
            public bool IsInstall { get; set; }
            public bool IsUpgrade { get; set; }
        }

        private static TrackerData LoadTrackerData()
        {
            var versionInstalls = RegistryManager.GetIntValues(n => n.StartsWith("v"));
            if (versionInstalls == null)
                return null; //cannot track

            var appVersionKey = "v" + AppVersion;
            const string ACTIVITY_DAYS_KEY = "ad";
            const string LAST_ACTIVITY_KEY = "la";
            var trackerData = new TrackerData
            {
                VersionInstalls = versionInstalls.ToDictionary(kv => kv.Key, kv => ToDate(kv.Value)),
                ActiveDays = RegistryManager.GetIntValue(ACTIVITY_DAYS_KEY) ?? 0,
                LastActivity = ToDate(RegistryManager.GetIntValue(LAST_ACTIVITY_KEY)),
                IsInstall = !versionInstalls.Any(),
                IsUpgrade = versionInstalls.Any() && !versionInstalls.ContainsKey(appVersionKey)
            };

            var today = DateTime.Today;
            if (!versionInstalls.ContainsKey(appVersionKey))
            {
                RegistryManager.SetInt(appVersionKey, FromDate(today));
            }

            if (trackerData.LastActivity == null || trackerData.LastActivity.Value < today)
            {
                trackerData.ActiveDays += 1;
                RegistryManager.SetInt(LAST_ACTIVITY_KEY, FromDate(today));
                RegistryManager.SetInt(ACTIVITY_DAYS_KEY, trackerData.ActiveDays);
            }

            return trackerData;
        }

        private static int FromDate(DateTime dateTime)
        {
            return (int)Math.Round((dateTime - MagicDate).TotalDays);
        }

        private static DateTime? ToDate(int? number)
        {
            if (number == null)
                return null;
            return ToDate(number.Value);
        }

        private static DateTime ToDate(int number)
        {
            return MagicDate.AddDays(number);
        }
    }
}
