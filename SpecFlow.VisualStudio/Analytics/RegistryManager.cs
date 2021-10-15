using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using Microsoft.Win32;

namespace SpecFlow.VisualStudio.Analytics
{
    public interface IRegistryManager
    {
        bool UpdateStatus(SpecFlowInstallationStatus status);
        SpecFlowInstallationStatus GetInstallStatus();
    }

    [Export(typeof(IRegistryManager))]
    public class RegistryManager : IRegistryManager
    {
#if DEBUG
        private const string REG_PATH = @"Software\TechTalk\SpecFlow\Debug";
#else
        private const string REG_PATH = @"Software\TechTalk\SpecFlow";
#endif
        private const string Version2019 = "version";
        private const string Version = "version.vs2022";
        private const string InstallDate = "installDate.vs2022";
        private const string LastUsedDate = "lastUsedDate";
        private const string UsageDays = "usageDays";
        private const string UserLevel = "userLevel";

        private string RegPath
        {
            get
            {
                var regPath = REG_PATH;
                return regPath;
            }
        }

        public SpecFlowInstallationStatus GetInstallStatus()
        {
            var status = new SpecFlowInstallationStatus();

            using (var key = Registry.CurrentUser.OpenSubKey(RegPath, RegistryKeyPermissionCheck.ReadSubTree))
            {
                if (key == null)
                    return status;
                
                status.Installed2019Version = ReadStringValue(key, Version2019, s => new Version(s)); 
                status.InstalledVersion = ReadStringValue(key, Version, s => new Version(s));
                status.InstallDate = ReadIntValue(key, InstallDate, DeserializeDate);
                status.LastUsedDate = ReadIntValue(key, LastUsedDate, DeserializeDate);
                status.UsageDays = ReadIntValue(key, UsageDays, i => i);
                status.UserLevel = ReadIntValue(key, UserLevel, i => i);
            }

            return status;
        }

        public bool UpdateStatus(SpecFlowInstallationStatus status)
        {
            using (var key = Registry.CurrentUser.CreateSubKey(RegPath, RegistryKeyPermissionCheck.ReadWriteSubTree))
            {
                if (key == null)
                    return false;

                if (status.InstalledVersion != null)
                    key.SetValue(Version, status.InstalledVersion);
                if (status.InstallDate != null)
                    key.SetValue(InstallDate, SerializeDate(status.InstallDate));
                if (status.LastUsedDate != null)
                    key.SetValue(LastUsedDate, SerializeDate(status.LastUsedDate));
                key.SetValue(UsageDays, status.UsageDays);
                key.SetValue(UserLevel, status.UserLevel);
            }
            return true;
        }

        private T ReadStringValue<T>(RegistryKey key, string name, Func<string, T> converter)
        {
            try
            {
                var value = key.GetValue(name) as string;
                if (string.IsNullOrEmpty(value))
                    return default(T);
                return converter(value);
            }
            catch(Exception ex)
            {
                Debug.WriteLine(ex, $"Registry read error: {this}");
                return default(T);
            }
        }

        private T ReadIntValue<T>(RegistryKey key, string name, Func<int, T> converter)
        {
            try
            {
                var value = key.GetValue(name);
                if (value == null || !(value is int))
                    return default(T);
                return converter((int)value);
            }
            catch(Exception ex)
            {
                Debug.WriteLine(ex, $"Registry read error: {this}");
                return default(T);
            }
        }

        private readonly DateTime magicDate = new DateTime(2009, 9, 11); //when SpecFlow has born
        private DateTime? DeserializeDate(int days)
        {
            if (days <= 0)
                return null;
            return magicDate.AddDays(days);
        }

        private int SerializeDate(DateTime? dateTime)
        {
            if (dateTime == null)
                return 0;

            return (int)dateTime.Value.Date.Subtract(magicDate).TotalDays;
        }
    }
}