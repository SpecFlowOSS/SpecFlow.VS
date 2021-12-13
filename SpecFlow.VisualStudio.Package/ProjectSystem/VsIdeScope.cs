﻿using System;
using System.Collections.Concurrent;
using System.ComponentModel.Composition;
using System.IO.Abstractions;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using SpecFlow.VisualStudio.Common;
using SpecFlow.VisualStudio.Diagnostics;
using SpecFlow.VisualStudio.Discovery;
using SpecFlow.VisualStudio.Monitoring;
using SpecFlow.VisualStudio.ProjectSystem.Actions;
using SpecFlow.VisualStudio.VsEvents;
using EnvDTE;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Events;
using Microsoft.VisualStudio.Text;
using Document = Microsoft.CodeAnalysis.Document;
using Project = EnvDTE.Project;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Editor;

namespace SpecFlow.VisualStudio.ProjectSystem
{
    [Export(typeof(VsIdeScope))]
    public class VsIdeScope : IVsIdeScope
    {
        public IServiceProvider ServiceProvider { get; }
        public DTE Dte { get; }
        
        private readonly IPersistentSpanFactory _persistentSpanFactory;

        private readonly IVsSolutionEventListener _solutionEventListener;
        private readonly UpdateSolutionEventsListener _updateSolutionEventsListener;
        private readonly DocumentEventsListener _documentEventsListener;

        private readonly ConcurrentDictionary<string, VsProjectScope> _projectScopes = new ConcurrentDictionary<string, VsProjectScope>(StringComparer.OrdinalIgnoreCase);

        private bool _acivityStarted = false;

        public DeveroomCompositeLogger CompositeLogger { get; } = new DeveroomCompositeLogger
        {
            new DeveroomDebugLogger(),
            new DeveroomFileLogger()
        };

        public bool IsSolutionLoaded { get; private set; } = false;

        public IDeveroomLogger Logger => CompositeLogger;
        public IMonitoringService MonitoringService { get; }
        public IIdeActions Actions { get; }
        public IDeveroomWindowManager WindowManager { get; }
        public IFileSystem FileSystem { get; }
        public IDeveroomOutputPaneServices DeveroomOutputPaneServices { get; }
        public IDeveroomErrorListServices DeveroomErrorListServices { get; }

        public event EventHandler<EventArgs> ProjectsBuilt;
        public event EventHandler<EventArgs> WeakProjectsBuilt
        {
            add => WeakEventManager<VsIdeScope, EventArgs>.AddHandler(this, nameof(ProjectsBuilt), value);
            remove => WeakEventManager<VsIdeScope, EventArgs>.RemoveHandler(this, nameof(ProjectsBuilt), value);
        }

        public event EventHandler<EventArgs> ProjectOutputsUpdated;
        public event EventHandler<EventArgs> WeakProjectOutputsUpdated
        {
            add => WeakEventManager<VsIdeScope, EventArgs>.AddHandler(this, nameof(ProjectOutputsUpdated), value);
            remove => WeakEventManager<VsIdeScope, EventArgs>.RemoveHandler(this, nameof(ProjectOutputsUpdated), value);
        }

        [ImportingConstructor]
        public VsIdeScope([Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider, IVsSolutionEventListener solutionEventListener, IMonitoringService monitoringService, IDeveroomWindowManager windowManager, IFileSystem fileSystem)
        {
            ServiceProvider = serviceProvider;
            MonitoringService = monitoringService;
            FileSystem = fileSystem;

            Dte = (DTE)serviceProvider.GetService(typeof(DTE));
            DeveroomOutputPaneServices = new VsDeveroomOutputPaneServices(this);
            DeveroomErrorListServices = new VsDeveroomErrorListServices(this);

            CompositeLogger.Add(new OutputWindowPaneLogger(DeveroomOutputPaneServices));
            Logger.LogVerbose("Creating IDE Scope");
            Actions = new VsIdeActions(this);
            
            _persistentSpanFactory = VsUtils.ResolveMefDependency<IPersistentSpanFactory>(serviceProvider);

            _solutionEventListener = solutionEventListener;
            _updateSolutionEventsListener = new UpdateSolutionEventsListener(serviceProvider, Logger, true);
            _updateSolutionEventsListener.BuildCompleted += UpdateSolutionEventsListenerOnBuildCompleted;

            _solutionEventListener.Loaded += SolutionEventListenerOnLoaded;
            _solutionEventListener.Closed += SolutionEventListenerOnClosed;
            _solutionEventListener.BeforeCloseProject += SolutionEventListenerOnBeforeCloseProject;

            _documentEventsListener = new DocumentEventsListener(Logger, Dte);

            WindowManager = windowManager;

            IsSolutionLoaded = Dte.Solution.IsOpen;
        }

        private void OnActivityStarted()
        {
            if (_acivityStarted)
                return;

            _acivityStarted = true;
            Logger.LogInfo("Starting Visual Studio Extension...");
            MonitoringService.MonitorOpenProjectSystem(this);
        }

        public void CalculateSourceLocationTrackingPositions(IEnumerable<SourceLocation> sourceLocations)
        {
            var editorAdaptersFactoryService = VsUtils.ResolveMefDependency<IVsEditorAdaptersFactoryService>(ServiceProvider);

            var sourceLocationsByFile = sourceLocations
                .Where(sl => sl.SourceLocationSpan == null)
                .GroupBy(sl => sl.SourceFile);

            int counter = 0;
            foreach (var sourceLocationsForFile in sourceLocationsByFile)
            {
                var sourceFile = sourceLocationsForFile.Key;
                var wpfTextView = VsUtils.GetWpfTextViewFromFilePath(sourceFile, ServiceProvider, editorAdaptersFactoryService);

                foreach (var sourceLocation in sourceLocationsForFile)
                {
                    counter++;
                    sourceLocation.SourceLocationSpan = this.CreatePersistentTrackingPosition(sourceLocation, wpfTextView);
                }
            }

            Logger.LogVerbose($"{counter} tracking positions calculated");
        }

        private IPersistentSpan CreatePersistentTrackingPosition(SourceLocation sourceLocation, IWpfTextView wpfTextView)
        {
            var line0 = sourceLocation.SourceFileLine - 1;
            var lineOffset = sourceLocation.SourceFileColumn - 1;
            var endLine0 = sourceLocation.SourceFileEndLine - 1 ?? line0;
            var endLineOffset = sourceLocation.SourceFileEndColumn - 1 ?? lineOffset;
            try
            {
                if (wpfTextView != null)
                    return _persistentSpanFactory.Create(wpfTextView.TextSnapshot, line0, lineOffset, endLine0, endLineOffset,
                        SpanTrackingMode.EdgeExclusive);

                return _persistentSpanFactory.Create(sourceLocation.SourceFile, line0, lineOffset, endLine0, endLineOffset,
                    SpanTrackingMode.EdgeExclusive);
            }
            catch (Exception ex)
            {
                Logger.LogException(MonitoringService, ex);
                return null;
            }
        }

        class DeveroomUndoContext : IDisposable
        {
            private DTE _dte;

            public DeveroomUndoContext(DTE dte, string undoLabel)
            {
                _dte = dte;
                _dte.UndoContext.Open(undoLabel);
            }

            public void Dispose()
            {
                _dte.UndoContext.Close();
            }
        }

        public IDisposable CreateUndoContext(string undoLabel)
        {
            return new DeveroomUndoContext(Dte, undoLabel);
        }

        public bool GetTextBuffer(SourceLocation sourceLocation, out ITextBuffer textBuffer)
        {
            if (sourceLocation.SourceLocationSpan?.IsDocumentOpen == true &&
                sourceLocation.SourceLocationSpan?.Document?.TextBuffer != null)
            {
                textBuffer = sourceLocation.SourceLocationSpan.Document.TextBuffer;
                return true;
            }

            var editorAdaptersFactoryService = VsUtils.ResolveMefDependency<IVsEditorAdaptersFactoryService>(ServiceProvider);

            var wpfTextView = VsUtils.GetWpfTextViewFromFilePath(sourceLocation.SourceFile, ServiceProvider, editorAdaptersFactoryService);
            textBuffer = wpfTextView?.TextBuffer;
            return textBuffer != null;
        }

        public SyntaxTree GetSyntaxTree(ITextBuffer textBuffer)
        {
            Document roslynDocument = textBuffer.GetRelatedDocuments().FirstOrDefault();
            if (roslynDocument != null && roslynDocument.TryGetSyntaxTree(out var syntaxTree))
                return syntaxTree;
            return null;
        }

        public Task RunOnBackgroundThread(Func<Task> action, Action<Exception> onException, [CallerMemberName] string callerName = "???")
        {
            return ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                try
                {
                    await action();
                }
                catch (Exception e)
                {
                    Logger.LogException(MonitoringService, e, $"Called from {callerName}");
                    onException(e);
                }
            }).Task;
        }

        public Task RunOnUiThread(Action action)
        {
            if (ThreadHelper.CheckAccess())
            {
                action();
                return Task.CompletedTask;
            }

            var task = ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                action();
            });
            return task.JoinAsync();
        }

        public void OpenIfNotOpened(string path)
        {
            VsUtils.OpenIfNotOpened(path, ServiceProvider);
        }

        private void SolutionEventListenerOnLoaded(object sender, EventArgs e)
        {
            Logger.LogVerbose("Solution loaded");
            IsSolutionLoaded = true;
        }

        private void SolutionEventListenerOnBeforeCloseProject(object sender, CloseProjectEventArgs e)
        {
            try
            {
                var projectPath = VsUtils.SafeGetProjectFilePath(e.Hierarchy);
                if (projectPath != null && _projectScopes.TryRemove(projectPath, out var projectScope))
                {
                    Logger.LogVerbose($"Closing project '{projectScope.ProjectName}'");
                    projectScope.Dispose();
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(MonitoringService, ex);
            }
            
        }

        private void SolutionEventListenerOnClosed(object sender, EventArgs e)
        {
            Logger.LogVerbose("Solution closed");
            foreach (var projectScope in _projectScopes.Values)
            {
                projectScope.Dispose();
            }
            _projectScopes.Clear();
            IsSolutionLoaded = false;
        }

        private void UpdateSolutionEventsListenerOnBuildCompleted(object sender, TestContainersChangedEventArgs e)
        {
            ProjectsBuilt?.Invoke(this, EventArgs.Empty);
            ProjectOutputsUpdated?.Invoke(this, EventArgs.Empty);
        }

        private string GetProjectId(Project project)
        {
            return project.FullName;
        }

        public IProjectScope GetProject(ITextBuffer textBuffer)
        {
            if (textBuffer == null) throw new ArgumentNullException(nameof(textBuffer));
            var project = VsUtils.GetProject(
                VsUtils.GetProjectItemFromTextBuffer(textBuffer));
            return GetProjectScope(project);
        }

        public IProjectScope GetProjectScope(Project project)
        {
            if (project == null ||
                !VsUtils.IsSolutionProject(project))
                return null;

            var projectId = GetProjectId(project);
            var projectScope = _projectScopes.GetOrAdd(projectId, id => CreateProjectScope(id, project));
            return projectScope;
        }

        private VsProjectScope CreateProjectScope(string id, Project project)
        {
            OnActivityStarted();
            Logger.LogInfo($"Initializing project: {project.Name}");
            var projectScope = new VsProjectScope(id, project, this);
            projectScope.InitializeServices();
            return projectScope;
        }

        public IProjectScope[] GetProjectsWithFeatureFiles()
        {
            try
            {
                return VsUtils.GetAllProjects(Dte)
                    .Where(HasFeatureFiles)
                    .Select(GetProjectScope)
                    .Where(ps => ps != null)
                    .ToArray();
            }
            catch (Exception e)
            {
                Logger.LogVerboseException(MonitoringService, e);
                return _projectScopes.Values.ToArray();
            }
        }

        private bool HasFeatureFiles(Project project)
        {
            try
            {
                if (!VsUtils.IsSolutionProject(project))
                    return false;
                return VsUtils.GetPhysicalFileProjectItems(project)
                    .Any(pi => FileSystemHelper.IsOfType(VsUtils.GetFilePath(pi), ".feature"));
            }
            catch (Exception e)
            {
                Logger.LogDebugException(e);
                return false;
            }
        }


        public void Dispose()
        {
            _updateSolutionEventsListener.BuildCompleted -= UpdateSolutionEventsListenerOnBuildCompleted;
            _updateSolutionEventsListener.Dispose();

            _solutionEventListener.Closed -= SolutionEventListenerOnClosed;
            (_solutionEventListener as IDisposable)?.Dispose();
            _documentEventsListener?.Dispose();
        }
    }
}
