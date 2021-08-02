using System;

namespace SpecFlow.VisualStudio.ProjectSystem
{
    public class NuGetVersion
    {
        public Version Version { get; }
        public string PreReleaseSuffix { get; }
        public bool IsPrerelease => PreReleaseSuffix != null;

        public NuGetVersion(string versionSpecifier)
        {
            if (versionSpecifier == null) throw new ArgumentNullException(nameof(versionSpecifier));

            var versionParts = versionSpecifier.Split(new[] {'-'}, 2);
            if (Version.TryParse(versionParts[0], out var version))
                Version = version;
            else
                Version = new Version();
            if (versionParts.Length > 1)
                PreReleaseSuffix = versionParts[1];
        }

        public override string ToString()
        {
            return IsPrerelease ? $"{Version}-{PreReleaseSuffix}" : Version.ToString();
        }

        public string ToShortVersionString()
        {
            return $"{Version.Major}{Version.Minor:00}{Version.Build}";
        }

        protected bool Equals(NuGetVersion other)
        {
            return Equals(Version, other.Version) && string.Equals(PreReleaseSuffix, other.PreReleaseSuffix);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((NuGetVersion) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Version != null ? Version.GetHashCode() : 0) * 397) ^ (PreReleaseSuffix != null ? PreReleaseSuffix.GetHashCode() : 0);
            }
        }
    }
}