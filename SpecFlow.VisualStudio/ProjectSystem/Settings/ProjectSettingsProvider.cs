﻿using System;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using SpecFlow.VisualStudio.Annotations;
using SpecFlow.VisualStudio.Diagnostics;
using SpecFlow.VisualStudio.Monitoring;
using SpecFlow.VisualStudio.ProjectSystem.Configuration;

namespace SpecFlow.VisualStudio.ProjectSystem.Settings
{
    public class ProjectSettingsProvider : IDisposable, IProjectSettingsProvider
    {
        private readonly IProjectScope _projectScope;
        private readonly SpecFlowProjectSettingsProvider _specFlowProjectSettingsProvider;
        private ProjectSettings _projectSettings;

        public const int MAX_RETRY_COUNT = 5;
        private DispatcherTimer _retryInitializeTimer;
        private int _retryInitializeCounter = 0;

        private IDeveroomLogger Logger => _projectScope.IdeScope.Logger;
        private IMonitoringService MonitoringService => _projectScope.IdeScope.MonitoringService;

        public event EventHandler<EventArgs> SettingsInitialized;
        public event EventHandler<EventArgs> WeakSettingsInitialized
        {
            add => WeakEventManager<IProjectSettingsProvider, EventArgs>.AddHandler(this, nameof(SettingsInitialized), value);
            remove => WeakEventManager<IProjectSettingsProvider, EventArgs>.RemoveHandler(this, nameof(SettingsInitialized), value);
        }

        public ProjectSettingsProvider([NotNull] IProjectScope projectScope, [NotNull] SpecFlowProjectSettingsProvider specFlowProjectSettingsProvider)
        {
            _projectScope = projectScope ?? throw new ArgumentNullException(nameof(projectScope));
            _specFlowProjectSettingsProvider = specFlowProjectSettingsProvider ?? throw new ArgumentNullException(nameof(specFlowProjectSettingsProvider));
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
            else
            {
                Logger.LogInfo($"Project settings updated: {projectSettings.GetShortLabel()}");
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

            var specFlowSettings = _specFlowProjectSettingsProvider.GetSpecFlowSettings(packageReferences);
            var hasFeatureFiles = (featureFileCount ?? 0) > 0;
            var kind = GetKind(isInvalid, specFlowSettings != null, hasFeatureFiles);
            var platformTarget = GetPlatformTarget(_projectScope.PlatformTargetName);
            
            var settings = new ProjectSettings(
                kind,
                _projectScope.OutputAssemblyPath,
                TargetFrameworkMoniker.Create(_projectScope.TargetFrameworkMoniker),
                _projectScope.TargetFrameworkMonikers,
                platformTarget,
                _projectScope.DefaultNamespace,
                specFlowSettings?.Version, 
                specFlowSettings?.GeneratorFolder,
                specFlowSettings?.ConfigFilePath,
                specFlowSettings?.Traits ?? SpecFlowProjectTraits.None,
                _projectScope.ProjectFullName);

            return settings;
        }

        private ProjectPlatformTarget GetPlatformTarget(string platformName)
        {
            if (platformName != null && Enum.TryParse<ProjectPlatformTarget>(platformName.Replace(" ", ""), true, out var platform))
                return platform;

            return ProjectPlatformTarget.Unknown;
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

        public void Dispose()
        {
            StopRetryInitializeTimer();
            _projectScope.GetDeveroomConfigurationProvider().WeakConfigurationChanged -= OnConfigurationChanged;
            _projectScope.IdeScope.WeakProjectsBuilt -= ProjectSystemOnProjectsBuilt;
        }
    }

    public interface IProjectSettingsProvider
    {
        event EventHandler<EventArgs> WeakSettingsInitialized;
        event EventHandler<EventArgs> SettingsInitialized;

        ProjectSettings GetProjectSettings();
        ProjectSettings CheckProjectSettings();
    }
}
