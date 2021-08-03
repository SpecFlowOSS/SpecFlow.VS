using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Win32;

namespace SpecFlow.VisualStudio.EventTracking
{
    public static class RegistryManager
    {
        public static void SetString(string name, string value)
        {
            try
            {
                using (var key = OpenKey())
                    key.SetValue(name, value, RegistryValueKind.String);
            }
            catch
            {
                Debug.WriteLine($"Could not store setting for '{name}'");
            }
        }

        public static void SetInt(string name, int value)
        {
            try
            {
                using (var key = OpenKey())
                    key.SetValue(name, value, RegistryValueKind.DWord);
            }
            catch
            {
                Debug.WriteLine($"Could not store setting for '{name}'");
            }
        }

        public static string GetStringValue(string name)
        {
            try
            {
                using (var key = OpenKey())
                    return key.GetValue(name) as string;
            }
            catch
            {
                Debug.WriteLine($"Could not load setting for '{name}'");
                return null;
            }
        }

        public static int? GetIntValue(string name)
        {
            try
            {
                using (var key = OpenKey())
                    return key.GetValue(name) as int?;
            }
            catch
            {
                Debug.WriteLine($"Could not load setting for '{name}'");
                return null;
            }
        }

        public static Dictionary<string,string> GetStringValues(Func<string, bool> keyFilter)
        {
            try
            {
                using (var key = OpenKey())
                    return key
                        .GetValueNames()
                        .Where(keyFilter)
                        .Select(n => new {Name = n, Value = key.GetValue(n) as string})
                        .Where(nv => !string.IsNullOrEmpty(nv.Value))
                        .ToDictionary(nv => nv.Name, nv => nv.Value);
            }
            catch
            {
                Debug.WriteLine($"Could not load values");
                return null;
            }
        }

        public static Dictionary<string,int> GetIntValues(Func<string, bool> keyFilter)
        {
            try
            {
                using (var key = OpenKey())
                    return key
                        .GetValueNames()
                        .Where(keyFilter)
                        .Select(n => new {Name = n, Value = key.GetValue(n) as int?})
                        .Where(nv => nv.Value != null)
                        .ToDictionary(nv => nv.Name, nv => nv.Value.Value);
            }
            catch
            {
                Debug.WriteLine($"Could not load values");
                return null;
            }
        }

        private static RegistryKey OpenKey()
        {
            const string configRoot = "Software\\Spec Solutions\\Deveroom";
            var key = Registry.CurrentUser.OpenSubKey(configRoot, RegistryKeyPermissionCheck.ReadWriteSubTree, System.Security.AccessControl.RegistryRights.FullControl);
            if (key == null) key = Registry.CurrentUser.CreateSubKey(configRoot, RegistryKeyPermissionCheck.ReadWriteSubTree);
            return key;
        }
    }
}
