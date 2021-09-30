using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SpecFlow.VisualStudio.Common;
using SpecFlow.VisualStudio.Configuration;
using SpecFlow.VisualStudio.ProjectSystem;
using SpecFlow.VisualStudio.ProjectSystem.Configuration;
using Microsoft.VisualStudio.Utilities;

namespace SpecFlow.VisualStudio.VsxStubs.ProjectSystem
{
    public class InMemoryStubProjectScope : IProjectScope
    {
        public DeveroomConfiguration DeveroomConfiguration { get; } = new DeveroomConfiguration();
        public PropertyCollection Properties { get; } = new PropertyCollection();
        public IIdeScope IdeScope => StubIdeScope;
        public StubIdeScope StubIdeScope {get; }
        public IEnumerable<NuGetPackageReference> PackageReferences => PackageReferencesList;
        public string ProjectFolder { get; } = Path.GetTempPath();
        public string OutputAssemblyPath => Path.Combine(ProjectFolder, "out.dll");
        public string TargetFrameworkMoniker { get; } = ".NETFramework,Version=v4.5.2";
        public string TargetFrameworkMonikers { get; } = ".NETCoreApp,Version=v5.0;.NETFramework,Version=v4.5.2";
        public string PlatformTargetName { get; } = "Any CPU";
        public string ProjectName { get; } = "Test Project";
        public string ProjectFullName { get; } = "Test Project.csproj";
        public string DefaultNamespace => ProjectName.Replace(" ", "");

        public List<NuGetPackageReference> PackageReferencesList = new List<NuGetPackageReference>();
        public Dictionary<string, string> FilesAdded { get; } = new Dictionary<string, string>();

        public InMemoryStubProjectScope(IIdeScope ideScope)
        {
            StubIdeScope = ideScope as StubIdeScope;
            Properties.AddProperty(typeof(IDeveroomConfigurationProvider), new StubDeveroomConfigurationProvider(DeveroomConfiguration));
            StubIdeScope.ProjectScopes.Add(this);
        }

        public void AddSpecFlowPackage()
        {
            PackageReferencesList.Add(new NuGetPackageReference("SpecFlow", new NuGetVersion("2.3.2", "2.3.2"), Path.Combine(ProjectFolder, "packages", "SpecFlow")));
        }

        public void AddFile(string targetFilePath, string template)
        {
            if (targetFilePath != null && !Path.IsPathRooted(targetFilePath))
                targetFilePath = Path.Combine(ProjectFolder, targetFilePath);

            StubIdeScope.FileSystem.Directory.CreateDirectory(Path.GetDirectoryName(targetFilePath));
            StubIdeScope.FileSystem.File.WriteAllText(targetFilePath, template);
            FilesAdded[targetFilePath] = template;
        }

        public int? GetFeatureFileCount()
        {
            return FilesAdded.Keys.Count(f => f.EndsWith(".feature"));
        }

        public string[] GetProjectFiles(string extension)
        {
            return FilesAdded.Keys.Where(f => FileSystemHelper.IsOfType(f, extension))
                .ToArray();
        }

        public void Dispose()
        {
        }
    }
}
