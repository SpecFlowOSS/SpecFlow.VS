using System;
using System.Collections.Generic;
using System.IO;
using Deveroom.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.Utilities;

namespace Deveroom.VisualStudio.VsxStubs.ProjectSystem
{
    public class StubProjectScope : IProjectScope
    {
        private readonly List<NuGetPackageReference> _packageReferences;

        public PropertyCollection Properties { get; } = new PropertyCollection();
        public IIdeScope IdeScope { get; }
        public IEnumerable<NuGetPackageReference> PackageReferences => _packageReferences;
        public string ProjectFolder { get; }
        public string OutputAssemblyPath { get; }
        public string TargetFrameworkMoniker { get; } = null;
        public string ProjectName { get; set; } = "Test Project";
        public string DefaultNamespace => ProjectName.Replace(" ", "");

        public StubProjectScope(string projectFolder, string outputAssemblyPath,
            IIdeScope ideScope, IEnumerable<NuGetPackageReference> packageReferences, string targetFramework)
        {
            ProjectFolder = projectFolder;
            IdeScope = ideScope;
            OutputAssemblyPath = Path.GetFullPath(Path.Combine(ProjectFolder, outputAssemblyPath));
            _packageReferences = new List<NuGetPackageReference>(packageReferences);

            if (targetFramework.StartsWith("netcoreapp"))
            {
                TargetFrameworkMoniker = $".NETCoreApp,Version=v{targetFramework.Substring("netcoreapp".Length)}";
            }
            else if (targetFramework.StartsWith("net"))
            {
                TargetFrameworkMoniker = $".NETFramework,Version=v{targetFramework[3]}.{targetFramework[4]}.{targetFramework[5]}";
            }
        }

        public void AddFile(string targetFilePath, string template)
        {
            throw new NotImplementedException();
        }

        public int? GetFeatureFileCount()
        {
            return IdeScope.FileSystem.Directory.GetFiles(ProjectFolder, "*.feature", SearchOption.AllDirectories).Length;
        }

        public string[] GetProjectFiles(string extension)
        {
            return IdeScope.FileSystem.Directory.GetFiles(ProjectFolder, "*" + (extension ?? ".*"), SearchOption.AllDirectories);
        }

        public void Dispose()
        {
        }
    }
}
