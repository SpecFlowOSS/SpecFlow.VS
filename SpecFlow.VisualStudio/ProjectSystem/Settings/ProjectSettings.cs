namespace SpecFlow.VisualStudio.ProjectSystem.Settings;

public record ProjectSettings(
    DeveroomProjectKind Kind,
    TargetFrameworkMoniker TargetFrameworkMoniker,
    string TargetFrameworkMonikers,
    ProjectPlatformTarget PlatformTarget,
    string OutputAssemblyPath,
    string DefaultNamespace,
    NuGetVersion SpecFlowVersion,
    string SpecFlowGeneratorFolder,
    string SpecFlowConfigFilePath,
    SpecFlowProjectTraits SpecFlowProjectTraits,
    ProjectProgrammingLanguage ProgrammingLanguage
)
{
    public bool IsUninitialized => Kind == DeveroomProjectKind.Uninitialized;
    public bool IsSpecFlowTestProject => Kind == DeveroomProjectKind.SpecFlowTestProject;
    public bool IsSpecFlowLibProject => Kind == DeveroomProjectKind.SpecFlowLibProject;
    public bool IsSpecFlowProject => IsSpecFlowTestProject || IsSpecFlowLibProject;

    public bool DesignTimeFeatureFileGenerationEnabled =>
        SpecFlowProjectTraits.HasFlag(SpecFlowProjectTraits.DesignTimeFeatureFileGeneration);

    public bool HasDesignTimeGenerationReplacement =>
        SpecFlowProjectTraits.HasFlag(SpecFlowProjectTraits.MsBuildGeneration) ||
        SpecFlowProjectTraits.HasFlag(SpecFlowProjectTraits.XUnitAdapter);

    public string GetSpecFlowVersionLabel() => SpecFlowVersion?.ToString() ?? "n/a";

    public string GetShortLabel()
    {
        var result = $"{TargetFrameworkMoniker},SpecFlow:{GetSpecFlowVersionLabel()}";
        if (PlatformTarget != ProjectPlatformTarget.Unknown && PlatformTarget != ProjectPlatformTarget.AnyCpu)
            result += "," + PlatformTarget;
        if (DesignTimeFeatureFileGenerationEnabled)
            result += ",Gen";
        return result;
    }
}
