namespace SpecFlow.VisualStudio.VsxStubs.ProjectSystem;

public class StubProjectSettingsProvider : Mock<IProjectSettingsProvider>, IProjectSettingsProvider
{
    public StubProjectSettingsProvider(InMemoryStubProjectScope inMemoryStubProjectScope) : base(MockBehavior.Strict)
    {
        OutputAssemblyPath = inMemoryStubProjectScope.OutputAssemblyPath;
        Setup(p => p.GetProjectSettings())
            .Returns(() => new ProjectSettings(Kind, OutputAssemblyPath, TargetFrameworkMoniker,
                TargetFrameworkMonikers,
                PlatformTarget, DefaultNamespace, SpecFlowVersion, SpecFlowGeneratorFolder, SpecFlowConfigFilePath,
                SpecFlowProjectTraits, ProjectFullName));
    }

    public DeveroomProjectKind Kind { get; set; } = DeveroomProjectKind.SpecFlowTestProject;

    public TargetFrameworkMoniker TargetFrameworkMoniker { get; set; } =
        TargetFrameworkMoniker.CreateFromShortName("net6");

    public string TargetFrameworkMonikers { get; set; } =
        $"{TargetFrameworkMoniker.CreateFromShortName("net6").Value};{TargetFrameworkMoniker.CreateFromShortName("net48").Value}";

    public ProjectPlatformTarget PlatformTarget { get; set; } = ProjectPlatformTarget.AnyCpu;
    public string OutputAssemblyPath { get; set; }
    public string DefaultNamespace { get; set; } = "TestProject";
    public NuGetVersion SpecFlowVersion { get; set; }
    public string SpecFlowGeneratorFolder { get; set; }
    public string SpecFlowConfigFilePath { get; set; }
    public SpecFlowProjectTraits SpecFlowProjectTraits { get; set; }
    public string ProjectFullName { get; set; } = "Test Project.csproj";
    public event EventHandler<EventArgs> WeakSettingsInitialized;
    public event EventHandler<EventArgs> SettingsInitialized;
    public ProjectSettings GetProjectSettings() => Object.GetProjectSettings();

    public ProjectSettings CheckProjectSettings() => throw new NotImplementedException();

    public void InvokeWeakSettingsInitializedEvent()
    {
        WeakSettingsInitialized!.Invoke(this, EventArgs.Empty);
    }
}
