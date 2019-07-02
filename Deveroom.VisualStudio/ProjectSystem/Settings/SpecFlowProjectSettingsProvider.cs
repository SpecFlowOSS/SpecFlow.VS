using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Deveroom.VisualStudio.Annotations;
using Deveroom.VisualStudio.ProjectSystem.Configuration;

namespace Deveroom.VisualStudio.ProjectSystem.Settings
{
    public class SpecFlowProjectSettingsProvider
    {
        private readonly IProjectScope _projectScope;

        public SpecFlowProjectSettingsProvider([NotNull] IProjectScope projectScope)
        {
            _projectScope = projectScope ?? throw new ArgumentNullException(nameof(projectScope));
        }

        public SpecFlowSettings GetSpecFlowSettings(IEnumerable<NuGetPackageReference> packageReferences)
        {
            var specFlowSettings =
                GetSpecFlowSettingsFromPackages(packageReferences) ??
                GetSpecFlowSettingsFromOutputFolder();

            return UpdateSpecFlowSettingsFromConfig(specFlowSettings);
        }

        private SpecFlowSettings UpdateSpecFlowSettingsFromConfig(SpecFlowSettings specFlowSettings)
        {
            var configuration = _projectScope.GetDeveroomConfiguration();
            if (configuration.SpecFlow.IsSpecFlowProject == null)
                return specFlowSettings;

            if (!configuration.SpecFlow.IsSpecFlowProject.Value)
                return null;

            specFlowSettings = specFlowSettings ?? new SpecFlowSettings();

            if (configuration.SpecFlow.Traits.Length > 0)
                foreach (var specFlowTrait in configuration.SpecFlow.Traits)
                    specFlowSettings.Traits |= specFlowTrait;

            if (configuration.SpecFlow.Version != null)
                specFlowSettings.Version = new NuGetVersion(configuration.SpecFlow.Version);

            if (configuration.SpecFlow.GeneratorFolder != null)
                specFlowSettings.GeneratorFolder = configuration.SpecFlow.GeneratorFolder;

            if (configuration.SpecFlow.ConfigFilePath != null)
                specFlowSettings.ConfigFilePath = configuration.SpecFlow.ConfigFilePath;

            return specFlowSettings;
        }

        private SpecFlowSettings GetSpecFlowSettingsFromPackages(IEnumerable<NuGetPackageReference> packageReferences)
        {
            var specFlowPackage = GetSpecFlowPackage(_projectScope, packageReferences, out var specFlowProjectTraits);
            if (specFlowPackage == null)
                return null;
            var specFlowVersion = specFlowPackage.Version;
            var specFlowGeneratorFolder = specFlowPackage.InstallPath == null
                ? null
                : Path.Combine(specFlowPackage.InstallPath, "tools");

            return CreateSpecFlowSettings(specFlowVersion, specFlowProjectTraits, specFlowGeneratorFolder);
        }

        private SpecFlowSettings GetSpecFlowSettingsFromOutputFolder()
        {
            var outputAssemblyPath = _projectScope.OutputAssemblyPath;
            if (outputAssemblyPath == null)
                return null;
            var outputFolder = Path.GetDirectoryName(_projectScope.OutputAssemblyPath);
            if (outputFolder == null)
                return null;

            var specFlowVersion = GetSpecFlowVersion(outputFolder);
            if (specFlowVersion == null)
                return null;

            var specFlowNuGetVersion = new NuGetVersion($"{specFlowVersion.FileMajorPart}.{specFlowVersion.FileMinorPart}.{specFlowVersion.FileBuildPart}");

            return CreateSpecFlowSettings(specFlowNuGetVersion, SpecFlowProjectTraits.None, null);
        }

        private FileVersionInfo GetSpecFlowVersion(string outputFolder)
        {
            var specFlowAssemblyPath = Path.Combine(outputFolder, "TechTalk.SpecFlow.dll");
            var fileVersionInfo = File.Exists(specFlowAssemblyPath) ? FileVersionInfo.GetVersionInfo(specFlowAssemblyPath) : null;
            return fileVersionInfo;
        }

        private SpecFlowSettings CreateSpecFlowSettings(NuGetVersion specFlowVersion, SpecFlowProjectTraits specFlowProjectTraits, string specFlowGeneratorFolder)
        {
            var configFilePath = GetSpecFlowConfigFilePath(_projectScope);

            if (specFlowVersion.Version < new Version(3, 0) &&
                !specFlowProjectTraits.HasFlag(SpecFlowProjectTraits.MsBuildGeneration) &&
                !specFlowProjectTraits.HasFlag(SpecFlowProjectTraits.XUnitAdapter))
                specFlowProjectTraits |= SpecFlowProjectTraits.DesignTimeFeatureFileGeneration;

            return new SpecFlowSettings(specFlowVersion, specFlowProjectTraits, specFlowGeneratorFolder, configFilePath);
        }

        private NuGetPackageReference GetSpecFlowPackage(IProjectScope projectScope, IEnumerable<NuGetPackageReference> packageReferences, out SpecFlowProjectTraits specFlowProjectTraits)
        {
            specFlowProjectTraits = SpecFlowProjectTraits.None;
            if (packageReferences == null)
                return null;
            var packageReferencesArray = packageReferences.ToArray();
            var detector = new SpecFlowPackageDetector(projectScope.IdeScope.FileSystem);
            var specFlowPackage = detector.GetSpecFlowPackage(packageReferencesArray);
            if (specFlowPackage != null)
            {
                if (detector.IsMsBuildGenerationEnabled(packageReferencesArray))
                    specFlowProjectTraits |= SpecFlowProjectTraits.MsBuildGeneration;
                if (detector.IsXUnitAdapterEnabled(packageReferencesArray))
                    specFlowProjectTraits |= SpecFlowProjectTraits.XUnitAdapter;
            }

            return specFlowPackage;
        }

        private string GetSpecFlowConfigFilePath(IProjectScope projectScope)
        {
            var projectFolder = projectScope.ProjectFolder;
            var fileSystem = projectScope.IdeScope.FileSystem;
            return fileSystem.GetFilePathIfExists(Path.Combine(projectFolder, ProjectScopeDeveroomConfigurationProvider.SpecFlowJsonConfigFileName)) ??
                   fileSystem.GetFilePathIfExists(Path.Combine(projectFolder, ProjectScopeDeveroomConfigurationProvider.SpecFlowAppConfigFileName));
        }

    }
}
