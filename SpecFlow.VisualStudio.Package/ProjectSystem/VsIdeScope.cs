using System;
using System.Collections.Concurrent;
using System.ComponentModel.Composition;
using System.IO.Abstractions;
using System.Linq;
using System.Windows;
using SpecFlow.VisualStudio.Common;
using SpecFlow.VisualStudio.Diagnostics;
using SpecFlow.VisualStudio.Discovery;
using SpecFlow.VisualStudio.Monitoring;
using SpecFlow.VisualStudio.ProjectSystem.Actions;
using SpecFlow.VisualStudio.ProjectSystem.Settings;
using SpecFlow.VisualStudio.UI;
using SpecFlow.VisualStudio.VsEvents;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Events;
using Microsoft.VisualStudio.Text;
using NuGet.VisualStudio;

namespace SpecFlow.VisualStudio.ProjectSystem
{
    [Export(typeof(VsIdeScope))]
    public class VsIdeScope : IVsIdeScope
    {
        public IServiceProvider ServiceProvider { get; }
        public DTE Dte { get; }

        private readonly IVsPackageInstallerServices _vsPackageInstallerServices;
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
        public VsIdeScope([Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider, IVsPackageInstallerServices vsPackageInstallerServices, IVsSolutionEventListener solutionEventListener, IMonitoringService monitoringService, IDeveroomWindowManager windowManager, IFileSystem fileSystem)
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

            _vsPackageInstallerServices = vsPackageInstallerServices;
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

        public IPersistentSpan CreatePersistentTrackingPosition(SourceLocation sourceLocation)
        {
            var line0 = sourceLocation.SourceFileLine - 1;
            var lineOffset = sourceLocation.SourceFileColumn - 1;
            var endLine0 = sourceLocation.SourceFileEndLine - 1 ?? line0;
            var endLineOffset = sourceLocation.SourceFileEndColumn - 1 ?? lineOffset;
            try
            {
                var wpfTextView = VsUtils.GetWpfTextViewFromFilePath(sourceLocation.SourceFile, ServiceProvider);
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
            var projectScope = new VsProjectScope(id, project, this, _vsPackageInstallerServices);
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
