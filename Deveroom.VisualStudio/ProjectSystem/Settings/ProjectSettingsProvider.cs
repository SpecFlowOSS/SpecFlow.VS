using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using Deveroom.VisualStudio.Annotations;
using Deveroom.VisualStudio.Diagonostics;
using Deveroom.VisualStudio.Monitoring;
using Deveroom.VisualStudio.ProjectSystem.Configuration;

namespace Deveroom.VisualStudio.ProjectSystem.Settings
{
    public class ProjectSettingsProvider : IDisposable
    {
        private readonly IProjectScope _projectScope;
        private ProjectSettings _projectSettings;

        public const int MAX_RETRY_COUNT = 5;
        private DispatcherTimer _retryInitializeTimer;
        private int _retryInitializeCounter = 0;

        private IDeveroomLogger Logger => _projectScope.IdeScope.Logger;
        private IMonitoringService MonitoringService => _projectScope.IdeScope.MonitoringService;

        public event EventHandler<EventArgs> SettingsInitialized;
        public event EventHandler<EventArgs> WeakSettingsInitialized
        {
            add => WeakEventManager<ProjectSettingsProvider, EventArgs>.AddHandler(this, nameof(SettingsInitialized), value);
            remove => WeakEventManager<ProjectSettingsProvider, EventArgs>.RemoveHandler(this, nameof(SettingsInitialized), value);
        }

        public ProjectSettingsProvider([NotNull] IProjectScope projectScope)
        {
            _projectScope = projectScope ?? throw new ArgumentNullException(nameof(projectScope));
            InitializeProjectSettings();

            _projectScope.GetDeveroomConfigurationProvider().WeakConfigurationChanged += OnConfigurationChanged;
            _projectScope.IdeScope.WeakProjectsBuilt += ProjectSystemOnProjectsBuilt;
        }

        private void InitializeProjectSettings()
        {
            _projectSettings = LoadProjectSettings(out var featureFileCount);
            if (!_projectSettings.IsUninitialized)
                OnSettingsInitialized(_projectSettings, featureFileCount);
            else
                StartRetryInitializeTimer();
        }

        private void StartRetryInitializeTimer()
        {
            _retryInitializeCounter++;
            Logger.LogInfo("Project settings not available yet, retry in 5 seconds...");
            _retryInitializeTimer = new DispatcherTimer(DispatcherPriority.ApplicationIdle)
            {
                Interval = TimeSpan.FromSeconds(5)
            };
            _retryInitializeTimer.Tick += RetryInitializeTimerTick;
            _retryInitializeTimer.Start();
        }

        private void StopRetryInitializeTimer()
        {
            if (_retryInitializeTimer == null)
                return;

            _retryInitializeTimer.Stop();
            _retryInitializeTimer.Tick -= RetryInitializeTimerTick;
            _retryInitializeTimer = null;
        }

        private void RetryInitializeTimerTick(object sender, EventArgs e)
        {
            StopRetryInitializeTimer();

            if (!_projectSettings.IsUninitialized)
                return;

            CheckProjectSettings();
            if (_projectSettings.IsUninitialized)
            {
                if (_retryInitializeCounter < MAX_RETRY_COUNT)
                    StartRetryInitializeTimer();
                else
                    Logger.LogInfo("Project settings could not be initialized. Rebuild the project to reload settings.");
            }
        }

        private void OnConfigurationChanged(object sender, EventArgs e)
        {
            CheckProjectSettings();
        }

        private void ProjectSystemOnProjectsBuilt(object sender, EventArgs e)
        {
            CheckProjectSettings();
        }

        public ProjectSettings GetProjectSettings()
        {
            return _projectSettings;
        }

        public ProjectSettings CheckProjectSettings()
        {
            var projectSettings = LoadProjectSettings(out var featureFileCount);
            if (projectSettings.IsUninitialized)
                return _projectSettings;

            if (projectSettings.Equals(_projectSettings))
                return _projectSettings;

            var wasUninitialized = _projectSettings.IsUninitialized;
            _projectSettings = projectSettings;
            if (wasUninitialized)
            {
                OnSettingsInitialized(projectSettings, featureFileCount);
            }
            return _projectSettings;
        }

        private void OnSettingsInitialized(ProjectSettings settings, int? featureFileCount)
        {
            MonitoringService.MonitorOpenProject(settings, featureFileCount);
            Logger.LogInfo($"Project settings initialized: {settings.GetShortLabel()}");
            SettingsInitialized?.Invoke(this, EventArgs.Empty);
        }

        private ProjectSettings LoadProjectSettings(out int? featureFileCount)
        {
            featureFileCount = _projectScope.GetFeatureFileCount();

            var packageReferences = _projectScope.PackageReferences;
            var isInvalid = packageReferences == null;

            var specFlowSettings = GetSpecFlowSettings(packageReferences);
            var hasFeatureFiles = (featureFileCount ?? 0) > 0;
            var kind = GetKind(isInvalid, specFlowSettings != null, hasFeatureFiles);

            var settings = new ProjectSettings(
                kind,
                _projectScope.OutputAssemblyPath,
                _projectScope.TargetFrameworkMoniker,
                _projectScope.DefaultNamespace,
                specFlowSettings?.Version, 
                specFlowSettings?.SpecFlowGeneratorFolder,
                GetSpecFlowConfigFilePath(_projectScope),
                specFlowSettings?.Traits ?? SpecFlowProjectTraits.None);

            return settings;
        }

        private DeveroomProjectKind GetKind(bool isInvalid, bool isSpecFlowProject, bool hasFeatureFiles)
        {
            if (isInvalid)
                return DeveroomProjectKind.Uninitialized;

            if (!isSpecFlowProject)
            {
                return hasFeatureFiles
                    ? DeveroomProjectKind.FeatureFileContainerProject
                    : DeveroomProjectKind.OtherProject;
            }

            return hasFeatureFiles
                ? DeveroomProjectKind.SpecFlowTestProject
                : DeveroomProjectKind.SpecFlowLibProject;
        }

        private SpecFlowSettings GetSpecFlowSettings(IEnumerable<NuGetPackageReference> packageReferences)
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
            if (specFlowVersion.Version < new Version(3, 0) &&
                !specFlowProjectTraits.HasFlag(SpecFlowProjectTraits.MsBuildGeneration) &&
                !specFlowProjectTraits.HasFlag(SpecFlowProjectTraits.XUnitAdapter))
                specFlowProjectTraits |= SpecFlowProjectTraits.DesignTimeFeatureFileGeneration;

            return new SpecFlowSettings(specFlowVersion, specFlowProjectTraits, specFlowGeneratorFolder);
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

        public void Dispose()
        {
            StopRetryInitializeTimer();
            _projectScope.GetDeveroomConfigurationProvider().WeakConfigurationChanged -= OnConfigurationChanged;
            _projectScope.IdeScope.WeakProjectsBuilt -= ProjectSystemOnProjectsBuilt;
        }
    }
}
