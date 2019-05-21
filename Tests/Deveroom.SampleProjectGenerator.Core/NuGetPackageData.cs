using System.IO;

namespace Deveroom.SampleProjectGenerator
{
    public class NuGetPackageData
    {
        public string PackageName { get; }
        public string Version { get; }
        public string InstallPath { get; }

        public NuGetPackageData(string packageName, string version, string installPath)
        {
            PackageName = packageName;
            Version = version;
            InstallPath = installPath?.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }
    }
}