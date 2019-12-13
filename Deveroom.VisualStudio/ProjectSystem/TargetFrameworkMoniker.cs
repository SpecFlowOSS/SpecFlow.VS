using System;
using System.Linq;

namespace Deveroom.VisualStudio.ProjectSystem
{
    public class TargetFrameworkMoniker
    {
        public const string NetCorePlatform = ".NETCoreApp";
        public const string NetFrameworkPlatform = ".NETFramework";
        private const string NetCoreShortValuePrefix = "netcoreapp";
        private const string NetFrameworkShortValuePrefix = "net";

        // e.g .NETCoreApp,Version=v2.1 or .NETFramework,Version=v4.5.2
        public string Value { private set; get; }

        public string Platform { private set; get; }
        public Version Version { private set; get; }

        public bool HasVersion => Version != null;
        public bool IsNetCore => NetCorePlatform.Equals(Platform);
        public bool IsNetFramework => NetFrameworkPlatform.Equals(Platform);

        private TargetFrameworkMoniker(string value)
        {
            Value = value;

            if (value != null)
            {
                var parts = value.Split(',');
                Platform = parts[0].Trim();
                const string versionPartPrefix = "Version=v";
                var versionPart = parts.FirstOrDefault(p => p.StartsWith(versionPartPrefix));
                if (versionPart != null)
                {
                    var versionString = versionPart.Substring(versionPartPrefix.Length);
                    if (Version.TryParse(versionString, out var version))
                        Version = version;
                }
            }
        }

        public static TargetFrameworkMoniker Create(string value)
        {
            return value == null ? null : new TargetFrameworkMoniker(value);
        }

        public static TargetFrameworkMoniker CreateFromShortName(string shortValue)
        {
            var value = shortValue;
            if (shortValue.StartsWith(NetCoreShortValuePrefix))
            {
                value = $".NETCoreApp,Version=v{shortValue.Substring(NetCoreShortValuePrefix.Length)}";
            }
            else if (shortValue.StartsWith(NetFrameworkShortValuePrefix))
            {
                value = $".NETFramework,Version=v{shortValue[NetFrameworkShortValuePrefix.Length]}.{shortValue[NetFrameworkShortValuePrefix.Length + 1]}.{shortValue[NetFrameworkShortValuePrefix.Length + 2]}";
            }
            return Create(value);
        }

        public override string ToString()
        {
            return Value;
        }

        public string ToShortString()
        {
            if (IsNetCore && HasVersion)
                return NetCoreShortValuePrefix + Version;
            if (IsNetFramework && HasVersion)
                return NetFrameworkShortValuePrefix + Version.ToString().Replace(".", "");
            return Value;
        }
    }
}
