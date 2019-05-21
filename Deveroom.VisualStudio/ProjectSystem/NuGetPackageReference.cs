using System;
using System.IO;
using System.Linq;

namespace Deveroom.VisualStudio.ProjectSystem
{
    public class NuGetPackageReference
    {
        public string PackageName { get; }
        public NuGetVersion Version { get; }
        public string InstallPath { get; }

        public NuGetPackageReference(string packageName, NuGetVersion version, string installPath)
        {
            PackageName = packageName;
            Version = version;
            InstallPath = installPath?.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        public override string ToString()
        {
            return $"{PackageName}/{Version}";
        }

        #region Equality

        protected bool Equals(NuGetPackageReference other)
        {
            return string.Equals(PackageName, other.PackageName) && Equals(Version, other.Version) && string.Equals(InstallPath, other.InstallPath);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((NuGetPackageReference) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (PackageName != null ? PackageName.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Version != null ? Version.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (InstallPath != null ? InstallPath.GetHashCode() : 0);
                return hashCode;
            }
        }

        #endregion
    }
}
