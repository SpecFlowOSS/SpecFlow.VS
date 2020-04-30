using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO.Abstractions;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Deveroom.VisualStudio.Diagonostics;
using Deveroom.VisualStudio.Monitoring;
using Deveroom.VisualStudio.ProjectSystem;
using Deveroom.VisualStudio.ProjectSystem.Configuration;
using Deveroom.VisualStudio.ProjectSystem.Settings;
using Microsoft.VisualStudio.Shell;

namespace Deveroom.VisualStudio.Discovery
{
    public class DiscoveryService : IDiscoveryService
    {
        enum DiscoveryStatus
        {
            Uninitialized,
            UninitializedProjectSettings,
            TestAssemblyNotFound,
            Error,
            Discovered,
            NonSpecFlowTestProject
        }

        private readonly IProjectScope _projectScope;
        private readonly IDiscoveryResultProvider _discoveryResultProvider;
        private readonly IDeveroomLogger _logger;
        private readonly IMonitoringService _monitoringService;
        private readonly ProjectSettingsProvider _projectSettingsProvider;
        private readonly IDeveroomErrorListServices _errorListServices;

        private bool _isDiscovering;
        private ProjectBindingRegistryCache _cached = new ProjectBindingRegistryCache(DiscoveryStatus.Uninitialized);
        private IFileSystem FileSystem => _projectScope.IdeScope.FileSystem;

        private class ProjectBindingRegistryCache
        {
            public DiscoveryStatus Status { get; }
            public ProjectBindingRegistry BindingRegistry { get; }
            public ProjectSettings ProjectSettings { get; }
            public DateTime? TestAssemblyWriteTimeUtc { get; }

            public ProjectBindingRegistryCache(DiscoveryStatus status, ProjectBindingRegistry bindingRegistry = null, ProjectSettings projectSettings = null, DateTime? testAssemblyWriteTimeUtc = null)
            {
                Status = status;
                BindingRegistry = bindingRegistry;
                ProjectSettings = projectSettings;
                TestAssemblyWriteTimeUtc = testAssemblyWriteTimeUtc;
            }
        }

        public event EventHandler<EventArgs> BindingRegistryChanged;
        public event EventHandler<EventArgs> WeakBindingRegistryChanged
        {
            add => WeakEventManager<DiscoveryService,EventArgs>.AddHandler(this, nameof(BindingRegistryChanged), value);
            remove => WeakEventManager<DiscoveryService, EventArgs>.RemoveHandler(this, nameof(BindingRegistryChanged), value);
        }

        public DiscoveryService(IProjectScope projectScope, IDiscoveryResultProvider discoveryResultProvider = null)
        {
            _projectScope = projectScope;
            _discoveryResultProvider = discoveryResultProvider ?? new DiscoveryResultProvider(_projectScope);
            _logger = _projectScope.IdeScope.Logger;
            _monitoringService = _projectScope.IdeScope.MonitoringService;
            _errorListServices = _projectScope.IdeScope.DeveroomErrorListServices;
            _projectSettingsProvider = _projectScope.GetProjectSettingsProvider();

            InitializeBindingRegistry();

            _projectSettingsProvider.WeakSettingsInitialized += ProjectSystemOnProjectsBuilt;
            _projectScope.IdeScope.WeakProjectOutputsUpdated += ProjectSystemOnProjectsBuilt;
        }

        private void InitializeBindingRegistry()
        {
            _logger.LogVerbose("Initial discovery triggered...");
            TriggerDiscovery();
        }

        private void ProjectSystemOnProjectsBuilt(object sender, EventArgs eventArgs)
        {
            _logger.LogVerbose("Projects built or settings initialized");
            CheckBindingRegistry();
        }

        public void CheckBindingRegistry()
        {
            if (IsCacheUpToDate() || _isDiscovering)
                return;

            TriggerDiscovery();
        }

        private bool IsCacheUpToDate()
        {
            var cached = _cached;
            if (cached.Status != DiscoveryStatus.Discovered &&
                cached.Status != DiscoveryStatus.NonSpecFlowTestProject)
                return false;

            var projectSettings = _projectScope.GetProjectSettings();
            var testAssemblySource = GetTestAssemblySource(projectSettings);
            if (!Equals(cached.ProjectSettings, projectSettings) ||
                cached.TestAssemblyWriteTimeUtc != testAssemblySource?.LastChangeTime)
                return false;

            return true;
        }

        protected virtual ConfigSource GetTestAssemblySource(ProjectSettings projectSettings)
        {
            return projectSettings.IsSpecFlowTestProject ? 
                ConfigSource.TryGetConfigSource(projectSettings.OutputAssemblyPath, FileSystem, _logger) : null;
        }

        public ProjectBindingRegistry GetBindingRegistry()
        {
            return _cached.BindingRegistry;
        }

        private void TriggerDiscovery()
        {
            var projectSettings = _projectScope.GetProjectSettings();
            ProjectBindingRegistryCache skippedResult = GetSkippedBindingRegistryResult(projectSettings, out var testAssemblySource);
            if (skippedResult != null)
            {
                PublishBindingRegistryResult(skippedResult);
                return;
            }

            TriggerDiscoveryOnBackgroundThread(projectSettings, testAssemblySource);
        }

        private ProjectBindingRegistryCache GetSkippedBindingRegistryResult(ProjectSettings projectSettings, out ConfigSource testAssemblySource)
        {
            testAssemblySource = null;

            if (projectSettings.IsUninitialized)
            {
                _logger.LogVerbose("Uninitialized project settings");
                return new ProjectBindingRegistryCache(DiscoveryStatus.UninitializedProjectSettings);
            }

            if (!projectSettings.IsSpecFlowTestProject)
            {
                _logger.LogVerbose("Non-SpecFlow test project");
                _logger.LogWarning($"Deveroom could not detect the SpecFlow version of the project that is required for navigation, step completion and other features. {Environment.NewLine}  Currently only NuGet package referenced can be detected. Please check https://github.com/specsolutions/deveroom-visualstudio/issues/14 for details.");
                return new ProjectBindingRegistryCache(DiscoveryStatus.NonSpecFlowTestProject, projectSettings: projectSettings);
            }

            testAssemblySource = GetTestAssemblySource(projectSettings);
            if (testAssemblySource == null)
            {
                _logger.LogInfo("Test assembly not found. Please build the project to enable Deveroom features.");
                return new ProjectBindingRegistryCache(DiscoveryStatus.TestAssemblyNotFound, projectSettings: projectSettings);
            }

            return null;
        }

        private void TriggerDiscoveryOnBackgroundThread(ProjectSettings projectSettings, ConfigSource testAssemblySource)
        {
            _isDiscovering = true;
            ThreadPool.QueueUserWorkItem(_ => DiscoveryOnBackgroundThread(projectSettings, testAssemblySource));
        }

        private void DiscoveryOnBackgroundThread(ProjectSettings projectSettings, ConfigSource testAssemblySource)
        {
            ProjectBindingRegistryCache projectBindingRegistryCache = null;
            try
            {
                projectBindingRegistryCache = InvokeDiscoveryWithTimer(projectSettings, testAssemblySource);
                PublishBindingRegistryResult(projectBindingRegistryCache);
            }
            catch (Exception ex)
            {
                _logger.LogException(_monitoringService, ex);
            }
            finally
            {
                while (_backgroundDiscoveryCompletionSources.TryDequeue(out var completionSource))
                    completionSource.SetResult(projectBindingRegistryCache?.BindingRegistry);
                _isDiscovering = false;
            }
        }

        private readonly ConcurrentQueue<TaskCompletionSource<ProjectBindingRegistry>>
            _backgroundDiscoveryCompletionSources = new ConcurrentQueue<TaskCompletionSource<ProjectBindingRegistry>>();

        public async Task<ProjectBindingRegistry> GetBindingRegistryAsync()
        {
            if (!_isDiscovering)
                return GetBindingRegistry();

            var completionSource = new TaskCompletionSource<ProjectBindingRegistry>();
            _backgroundDiscoveryCompletionSources.Enqueue(completionSource);

            if (!_isDiscovering) // in case it finished discovery while we were registering the completion source
                return GetBindingRegistry();

            return await completionSource.Task;
        }

        private ProjectBindingRegistryCache InvokeDiscoveryWithTimer(ProjectSettings projectSettings, ConfigSource testAssemblySource)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var result = InvokeDiscovery(projectSettings, testAssemblySource);
            stopwatch.Stop();
            if (result.Status == DiscoveryStatus.Discovered)
                _logger.LogVerbose($"Discovery: {stopwatch.ElapsedMilliseconds} ms");
            return result;
        }

        private ProjectBindingRegistryCache InvokeDiscovery(ProjectSettings projectSettings, ConfigSource testAssemblySource)
        {
            try
            {
                _errorListServices?.ClearErrors(DeveroomUserErrorCategory.Discovery);

                var result = _discoveryResultProvider.RunDiscovery(testAssemblySource.FilePath, projectSettings.SpecFlowConfigFilePath, projectSettings);
                var bindingRegistry = new ProjectBindingRegistry();
                if (result.IsFailed)
                {
                    bindingRegistry.IsFailed = true;
                    _logger.LogWarning(result.ErrorMessage);
                    _logger.LogWarning($"The project bindings (e.g. step definitions) could not be discovered. Navigation, step completion and other features are disabled. {Environment.NewLine}  Please check the error message above and report to https://github.com/specsolutions/deveroom-visualstudio/issues if you cannot fix.");
                }
                else
                {
                    var bindingImporter = new BindingImporter(result.SourceFiles, result.TypeNames, _logger);

                    bindingRegistry.StepDefinitions = result.StepDefinitions
                        .Select(sd => bindingImporter.ImportStepDefinition(sd))
                        .Where(psd => psd != null)
                        .ToArray();
                    _logger.LogInfo(
                        $"{bindingRegistry.StepDefinitions.Length} step definitions discovered for project {_projectScope.ProjectName}");

                    if (bindingRegistry.StepDefinitions.Any(sd => !sd.IsValid))
                    {
                        _logger.LogWarning($"Invalid step definitions found: {Environment.NewLine}" + 
                                           string.Join(Environment.NewLine, bindingRegistry.StepDefinitions.Where(sd => !sd.IsValid)
                                               .Select(sd => $"  {sd}: {sd.Error} at {sd.Implementation?.SourceLocation}")));

                        _errorListServices?.AddErrors(
                            bindingRegistry.StepDefinitions.Where(sd => !sd.IsValid)
                                .Select(sd => new DeveroomUserError
                                {
                                    Category = DeveroomUserErrorCategory.Discovery,
                                    Message = sd.Error,
                                    SourceLocation = sd.Implementation?.SourceLocation,
                                    Type = TaskErrorCategory.Error
                                })
                            );
                    }

                    CalculateSourceLocationTrackingPositions(bindingRegistry);
                }

                _monitoringService.MonitorSpecFlowDiscovery(bindingRegistry.IsFailed, result.ErrorMessage, bindingRegistry.StepDefinitions.Length, projectSettings);
                return new ProjectBindingRegistryCache(DiscoveryStatus.Discovered, bindingRegistry, projectSettings, testAssemblySource.LastChangeTime);
            }
            catch (Exception ex)
            {
                _logger.LogException(_monitoringService, ex);
                return new ProjectBindingRegistryCache(DiscoveryStatus.Error);
            }
        }

        private void PublishBindingRegistryResult(ProjectBindingRegistryCache projectBindingRegistryCache)
        {
            Thread.MemoryBarrier();
            var oldCached = _cached;
            _cached = projectBindingRegistryCache;
            DisposeSourceLocationTrackingPositions(oldCached?.BindingRegistry);
            TriggerBindingRegistryChanged();
        }

        protected virtual void TriggerBindingRegistryChanged()
        {
            BindingRegistryChanged?.Invoke(this, new EventArgs());
        }

        private void CalculateSourceLocationTrackingPositions(ProjectBindingRegistry bindingRegistry)
        {
            int counter = 0;
            foreach (var sourceLocation in bindingRegistry.StepDefinitions.Select(sd => sd.Implementation.SourceLocation)
                .Where(sl => sl != null))
            {
                if (sourceLocation.SourceLocationSpan == null)
                {
                    counter++;
                    sourceLocation.SourceLocationSpan = _projectScope.IdeScope.CreatePersistentTrackingPosition(sourceLocation);
                }
            }

            _logger.LogVerbose($"{counter} tracking positions calculated");
        }

        private void DisposeSourceLocationTrackingPositions(ProjectBindingRegistry bindingRegistry)
        {
            if (bindingRegistry == null)
                return;
            foreach (var sourceLocation in bindingRegistry.StepDefinitions.Select(sd => sd.Implementation.SourceLocation)
                .Where(sl => sl?.SourceLocationSpan != null))
            {
                sourceLocation.SourceLocationSpan.Dispose();
                sourceLocation.SourceLocationSpan = null;
            }
            _logger.LogVerbose($"Tracking positions disposed");
        }

        public void Dispose()
        {
            _projectScope.IdeScope.WeakProjectsBuilt -= ProjectSystemOnProjectsBuilt;
            _projectSettingsProvider.SettingsInitialized -= ProjectSystemOnProjectsBuilt;
        }
    }
}
