using System;

namespace Deveroom.VisualStudio.ProjectSystem.Settings
{
    public class ProjectSettings
    {
        public DeveroomProjectKind Kind { get; }
        public string TargetFrameworkMoniker { get; }
        public string OutputAssemblyPath { get; }
        public string DefaultNamespace { get; }
        public NuGetVersion SpecFlowVersion { get; }
        public string SpecFlowGeneratorFolder { get; }
        public string SpecFlowConfigFilePath { get; }
        public SpecFlowProjectTraits SpecFlowProjectTraits { get; }

        public ProjectSettings(DeveroomProjectKind kind, string outputAssemblyPath, string targetFrameworkMoniker, string defaultNamespace,
            NuGetVersion specFlowVersion, string specFlowGeneratorFolder, string specFlowConfigFilePath, SpecFlowProjectTraits specFlowProjectTraits)
        {
            Kind = kind;
            TargetFrameworkMoniker = targetFrameworkMoniker;
            OutputAssemblyPath = outputAssemblyPath;
            DefaultNamespace = defaultNamespace;
            SpecFlowVersion = specFlowVersion;
            SpecFlowGeneratorFolder = specFlowGeneratorFolder;
            SpecFlowConfigFilePath = specFlowConfigFilePath;
            SpecFlowProjectTraits = specFlowProjectTraits;
        }

        public bool IsUninitialized => Kind == DeveroomProjectKind.Uninitialized;
        public bool IsSpecFlowTestProject => Kind == DeveroomProjectKind.SpecFlowTestProject;
        public bool IsSpecFlowLibProject => Kind == DeveroomProjectKind.SpecFlowLibProject;
        public bool IsSpecFlowProject => IsSpecFlowTestProject || IsSpecFlowLibProject;
        public bool DesignTimeFeatureFileGenerationEnabled => SpecFlowProjectTraits.HasFlag(SpecFlowProjectTraits.DesignTimeFeatureFileGeneration);
        public bool HasDesignTimeGenerationReplacement => SpecFlowProjectTraits.HasFlag(SpecFlowProjectTraits.MsBuildGeneration) || SpecFlowProjectTraits.HasFlag(SpecFlowProjectTraits.XUnitAdapter);

        public string GetSpecFlowVersionLabel()
        {
            return SpecFlowVersion?.ToString() ?? "n/a";
        }

        public string GetShortLabel()
        {
            var result = $"{TargetFrameworkMoniker},SpecFlow:{GetSpecFlowVersionLabel()}";
            if (DesignTimeFeatureFileGenerationEnabled)
                result += ",Gen";
            return result;
        }

        #region Equality

        protected bool Equals(ProjectSettings other)
        {
            return Kind == other.Kind && string.Equals(TargetFrameworkMoniker, other.TargetFrameworkMoniker) && string.Equals(OutputAssemblyPath, other.OutputAssemblyPath) && string.Equals(DefaultNamespace, other.DefaultNamespace) && Equals(SpecFlowVersion, other.SpecFlowVersion) && string.Equals(SpecFlowGeneratorFolder, other.SpecFlowGeneratorFolder) && string.Equals(SpecFlowConfigFilePath, other.SpecFlowConfigFilePath) && SpecFlowProjectTraits == other.SpecFlowProjectTraits;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((ProjectSettings) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (int) Kind;
                hashCode = (hashCode * 397) ^ (TargetFrameworkMoniker != null ? TargetFrameworkMoniker.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (OutputAssemblyPath != null ? OutputAssemblyPath.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (DefaultNamespace != null ? DefaultNamespace.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (SpecFlowVersion != null ? SpecFlowVersion.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (SpecFlowGeneratorFolder != null ? SpecFlowGeneratorFolder.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (SpecFlowConfigFilePath != null ? SpecFlowConfigFilePath.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (int) SpecFlowProjectTraits;
                return hashCode;
            }
        }

        #endregion
    }
}
