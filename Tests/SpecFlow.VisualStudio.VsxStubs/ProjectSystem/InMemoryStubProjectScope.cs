#nullable enable

namespace SpecFlow.VisualStudio.VsxStubs.ProjectSystem;

public class InMemoryStubProjectScope : Mock<IProjectScope>, IProjectScope
{
    public List<NuGetPackageReference> PackageReferencesList = new();

    public InMemoryStubProjectScope(StubIdeScope stubIdeScope)
    {
        StubIdeScope = stubIdeScope;

        StubProjectSettingsProvider = new StubProjectSettingsProvider(this);
        Properties.AddProperty(typeof(IProjectSettingsProvider), StubProjectSettingsProvider);
        StubDeveroomConfigurationProvider = new StubDeveroomConfigurationProvider(DeveroomConfiguration);
        Properties.AddProperty(typeof(IDeveroomConfigurationProvider), StubDeveroomConfigurationProvider);
        StubIdeScope.ProjectScopes.Add(this);

        AddFile(ProjectFullPath, "<Project Sdk=\"Microsoft.NET.Sdk\"></Project>");

        Build();
    }

    public InMemoryStubProjectScope(ITestOutputHelper testOutputHelper)
        : this(new StubIdeScope(testOutputHelper))
    {
    }

    public StubDeveroomConfigurationProvider StubDeveroomConfigurationProvider { get; }
    public DeveroomConfiguration DeveroomConfiguration { get; } = new();
    public StubIdeScope StubIdeScope { get; }
    public StubProjectSettingsProvider StubProjectSettingsProvider { get; }
    public Dictionary<string, string> FilesAdded { get; } = new();
    public string ProjectFullPath => Path.Combine(ProjectFolder, ProjectFullName);
    public PropertyCollection Properties { get; } = new();
    public IIdeScope IdeScope => StubIdeScope;
    public IEnumerable<NuGetPackageReference> PackageReferences => PackageReferencesList;
    public string ProjectFolder { get; } = Path.GetTempPath();
    public string OutputAssemblyPath => Path.Combine(ProjectFolder, "out.dll");
    public string TargetFrameworkMoniker { get; } = ".NETFramework,Version=v4.5.2";
    public string TargetFrameworkMonikers { get; } = ".NETCoreApp,Version=v5.0;.NETFramework,Version=v4.5.2";
    public string PlatformTargetName { get; } = "Any CPU";
    public string ProjectName { get; } = "Test Project";
    public string ProjectFullName { get; } = "Test Project.csproj";
    public string DefaultNamespace => ProjectName.Replace(" ", "");

    public void AddFile(string targetFilePath, string template)
    {
        if (!Path.IsPathRooted(targetFilePath))
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

    public void AddSpecFlowPackage()
    {
        PackageReferencesList.Add(new NuGetPackageReference("SpecFlow", new NuGetVersion("2.3.2", "2.3.2"),
            Path.Combine(ProjectFolder, "packages", "SpecFlow")));
    }

    public void Build()
    {
        FileInfo fi = new FileInfo(OutputAssemblyPath);
        StubIdeScope.FileSystem.Directory.CreateDirectory(fi.DirectoryName);

        StubIdeScope.FileSystem.File.WriteAllText(fi.FullName, string.Empty);
        var file = new MockFile(StubIdeScope.FileSystem as MockFileSystem);
        file.SetLastWriteTimeUtc(fi.FullName, DateTime.UtcNow);

        StubIdeScope.TriggerProjectsBuilt();
    }
}
