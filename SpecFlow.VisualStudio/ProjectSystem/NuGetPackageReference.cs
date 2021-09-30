using System.IO;

namespace SpecFlow.VisualStudio.ProjectSystem
{
    public record NuGetPackageReference
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
    }
}
