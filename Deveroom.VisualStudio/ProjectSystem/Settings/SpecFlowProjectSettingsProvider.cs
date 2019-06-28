using System;
using System.Collections.Generic;
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
            return
                GetSpecFlowSettingsFromConfig() ??
                GetSpecFlowSettingsFromPackages(packageReferences);
        }

        private SpecFlowSettings GetSpecFlowSettingsFromConfig()
        {
            var configuration = _projectScope.GetDeveroomConfiguration();
            if (!configuration.SpecFlow.IsSpecFlowProject)
                return null;

            var traits = SpecFlowProjectTraits.None;
            foreach (var specFlowTrait in configuration.SpecFlow.Traits)
            {
                traits |= specFlowTrait;
            }

            //TODO: handle partial config
            return new SpecFlowSettings(
                new NuGetVersion(configuration.SpecFlow.Version), 
                traits,
                configuration.SpecFlow.GeneratorFolder,
                configuration.SpecFlow.ConfigFilePath);
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
