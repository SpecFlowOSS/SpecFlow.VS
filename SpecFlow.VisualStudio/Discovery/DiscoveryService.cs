using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Abstractions;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SpecFlow.VisualStudio.Diagnostics;
using SpecFlow.VisualStudio.Monitoring;
using SpecFlow.VisualStudio.ProjectSystem;
using SpecFlow.VisualStudio.ProjectSystem.Configuration;
using SpecFlow.VisualStudio.ProjectSystem.Settings;
using Microsoft.VisualStudio.Shell;
using SpecFlow.VisualStudio.Editor.Commands;
using SpecFlow.VisualStudio.Editor.Services.Parser;
using SpecFlow.VisualStudio.SpecFlowConnector.Models;

namespace SpecFlow.VisualStudio.Discovery
{
    public class DiscoveryService : IDiscoveryService
    {
        private readonly IProjectScope _projectScope;
        private readonly IDiscoveryResultProvider _discoveryResultProvider;
        private readonly IDeveroomLogger _logger;
        private readonly IMonitoringService _monitoringService;
        private readonly ProjectSettingsProvider _projectSettingsProvider;
        private readonly IDeveroomErrorListServices _errorListServices;

        private bool _isDiscovering;
        private ProjectBindingRegistryCache _cached = new ProjectBindingRegistryCacheUninitialized();
        private IFileSystem FileSystem => _projectScope.IdeScope.FileSystem;

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
            _projectSettingsProvider.WeakSettingsInitialized += ProjectSystemOnProjectsBuilt;
            _projectScope.IdeScope.WeakProjectOutputsUpdated += ProjectSystemOnProjectsBuilt;
        }

        public void InitializeBindingRegistry()
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
            var projectSettings = _projectScope.GetProjectSettings();
            var testAssemblySource = GetTestAssemblySource(projectSettings);
            return _cached.IsUpToDate(projectSettings, testAssemblySource.LastChangeTime);
        }

        protected virtual ConfigSource GetTestAssemblySource(ProjectSettings projectSettings)
        {
            return projectSettings.IsSpecFlowTestProject ? 
                ConfigSource.TryGetConfigSource(projectSettings.OutputAssemblyPath, FileSystem, _logger) : ConfigSource.Invalid;
        }

        public ProjectBindingRegistry GetBindingRegistry()
        {
            return _cached.BindingRegistry;
        }

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

        private void TriggerDiscovery()
        {
            var projectSettings = _projectScope.GetProjectSettings();
            _errorListServices?.ClearErrors(DeveroomUserErrorCategory.Discovery);
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
                return new ProjectBindingRegistryCacheUninitializedProjectSettings();
            }

            if (!projectSettings.IsSpecFlowTestProject)
            {
                _logger.LogVerbose("Non-SpecFlow test project");
                if (_cached is ProjectBindingRegistryCacheUninitialized)
                    _logger.LogWarning($"Could not detect the SpecFlow version of the project that is required for navigation, step completion and other features. {Environment.NewLine}  Currently only NuGet package referenced can be detected. Please check https://github.com/specsolutions/deveroom-visualstudio/issues/14 for details.");
                return new ProjectBindingRegistryCacheNonSpecFlowTestProject();
            }

            testAssemblySource = GetTestAssemblySource(projectSettings);
            if (testAssemblySource == null)
            {
                var message = "Test assembly not found. Please build the project to enable the SpecFlow Visual Studio Extension features.";
                _logger.LogInfo(message);
                _errorListServices?.AddErrors(new []{ new DeveroomUserError
                {
                    Category = DeveroomUserErrorCategory.Discovery,
                    Message = message,
                    Type = TaskErrorCategory.Warning
                }});
                return new ProjectBindingRegistryCacheTestAssemblyNotFound();
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

        private ProjectBindingRegistryCache InvokeDiscoveryWithTimer(ProjectSettings projectSettings, ConfigSource testAssemblySource)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var result = InvokeDiscovery(projectSettings, testAssemblySource);
            stopwatch.Stop();
            if (result.IsDiscovered)
                _logger.LogVerbose($"Discovery: {stopwatch.ElapsedMilliseconds} ms");
            return result;
        }

        private ProjectBindingRegistryCache InvokeDiscovery(ProjectSettings projectSettings, ConfigSource testAssemblySource)
        {
            try
            {
                var result = _discoveryResultProvider.RunDiscovery(testAssemblySource.FilePath, projectSettings.SpecFlowConfigFilePath, projectSettings);
                ProjectBindingRegistry bindingRegistry = null;
                if (result.IsFailed)
                {
                    bindingRegistry = ProjectBindingRegistry.Invalid;
                    _logger.LogWarning(result.ErrorMessage);
                    _logger.LogWarning($"The project bindings (e.g. step definitions) could not be discovered. Navigation, step completion and other features are disabled. {Environment.NewLine}  Please check the error message above and report to https://github.com/SpecFlowOSS/SpecFlow.VS/issues if you cannot fix.");

                    _errorListServices?.AddErrors(new[]{ new DeveroomUserError
                    {
                        Category = DeveroomUserErrorCategory.Discovery,
                        Message = "The project bindings (e.g. step definitions) could not be discovered. Navigation, step completion and other features are disabled.",
                        Type = TaskErrorCategory.Warning
                    }});
                }
                else
                {
                    var bindingImporter = new BindingImporter(result.SourceFiles, result.TypeNames, _logger);

                    var stepDefinitions = result.StepDefinitions
                        .Select(sd => bindingImporter.ImportStepDefinition(sd))
                        .Where(psd => psd != null)
                        .ToArray();
                    bindingRegistry = new ProjectBindingRegistry(stepDefinitions);
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
                return new ProjectBindingRegistryCacheDiscovered(bindingRegistry, projectSettings, testAssemblySource.LastChangeTime);
            }
            catch (Exception ex)
            {
                _logger.LogException(_monitoringService, ex);
                return new ProjectBindingRegistryCacheError();
            }
        }

        public void ReplaceBindingRegistry(ProjectBindingRegistry bindingRegistry)
        {
            CalculateSourceLocationTrackingPositions(bindingRegistry);
            var newBindingRegistryCache = _cached.WithBindingRegistry(bindingRegistry);
            PublishBindingRegistryResult(newBindingRegistryCache);
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

        public async Task ProcessAsync(CSharpStepDefinitionFile stepDefinitionFile)
        {
            SyntaxTree tree = CSharpSyntaxTree.ParseText(stepDefinitionFile.Content);
            var rootNode = await tree.GetRootAsync();

            var allMethods = rootNode
                .DescendantNodes()
                .OfType<MethodDeclarationSyntax>()
                .ToArray();

            var projectStepDefinitionBindings = new List<ProjectStepDefinitionBinding>(allMethods.Length);
            foreach (MethodDeclarationSyntax method in allMethods)
            {
                var attributes = RenameStepStepDefinitionClassAction.GetAttributesWithTokens(method);

                var methodBodyBeginToken = method.Body.GetFirstToken();
                var methodBodyBeginPosition = methodBodyBeginToken.GetLocation().GetLineSpan().StartLinePosition;
                var methodBodyEndToken = method.Body.GetLastToken();
                var methodBodyEndPosition = methodBodyEndToken.GetLocation().GetLineSpan().StartLinePosition;

                Scope scope = null;
                var parameterTypes = method.ParameterList.Parameters
                    .Select(p => p.Type.ToString())
                    .ToArray();

                var sourceLocation = new SourceLocation(stepDefinitionFile.StepDefinitionPath, methodBodyBeginPosition.Line+1, 
                    methodBodyBeginPosition.Character+1, 
                    methodBodyEndPosition.Line + 1, 
                    methodBodyEndPosition.Character + 1);
                var implementation = new ProjectStepDefinitionImplementation(FullMethodName(method), parameterTypes, sourceLocation);

                foreach (var (attribute, token) in attributes)
                {
                    var stepDefinitionType = (ScenarioBlock)Enum.Parse(typeof(ScenarioBlock), attribute.Name.ToString());
                    var regex = new Regex($"^{token.ValueText}$");

                    var stepDefinitionBinding = new ProjectStepDefinitionBinding(stepDefinitionType, regex, scope, implementation, token.ValueText);

                    projectStepDefinitionBindings.Add(stepDefinitionBinding);
                }
            }
            var bindingRegistry = await GetBindingRegistryAsync();
            bindingRegistry = bindingRegistry
                .Where(binding=>binding.Implementation.SourceLocation.SourceFile != stepDefinitionFile.StepDefinitionPath)
                .AddStepDefinitions(projectStepDefinitionBindings);
            ReplaceBindingRegistry(bindingRegistry);
        }

        private static string FullMethodName(MethodDeclarationSyntax method)
        {
            StringBuilder sb = new StringBuilder();
            var containingClass = method.Parent as ClassDeclarationSyntax;
            if (containingClass.Parent is BaseNamespaceDeclarationSyntax namespaceSyntax)
            {
                var containingNamespace = namespaceSyntax.Name;
                sb.Append(containingNamespace).Append('.');
            }

            sb.Append(containingClass.Identifier.Text).Append('.').Append(method.Identifier.Text);
            return sb.ToString();
        }
    }
}
