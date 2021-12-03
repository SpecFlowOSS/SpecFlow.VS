using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Threading;
using System.Threading.Tasks;
using SpecFlow.VisualStudio.Diagnostics;
using SpecFlow.VisualStudio.Discovery;
using SpecFlow.VisualStudio.Monitoring;
using SpecFlow.VisualStudio.ProjectSystem.Actions;
using SpecFlow.VisualStudio.ProjectSystem.Settings;
using SpecFlow.VisualStudio.UI;
using SpecFlow.VisualStudio.UI.ViewModels;
using EnvDTE;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Text;
using SpecFlow.VisualStudio.Editor.Commands;
using SpecFlow.VisualStudio.Notifications;
using Project = EnvDTE.Project;

namespace SpecFlow.VisualStudio.ProjectSystem
{
    public class NullVsIdeScope : IVsIdeScope
    {
        class NullIdeActions : VsIdeActionsBase, IIdeActions, IAsyncContextMenu
        {
            public NullIdeActions(IVsIdeScope ideScope) : base(ideScope)
            {
            }

            public bool NavigateTo(SourceLocation sourceLocation)
            {
                return false;
            }

            public IAsyncContextMenu ShowContextMenu(string header, bool async,
                params ContextMenuItem[] contextMenuItems)
            {
                return this;
            }

            public CancellationToken CancellationToken { get; } = new CancellationToken();
            public bool IsComplete => true;

            public void AddItems(params ContextMenuItem[] items)
            {
                //nop
            }

            public void Complete()
            {
                //nop
            }
        }

        class NullMonitoringService : IMonitoringService
        {
            public void MonitorLoadProjectSystem()
            {
            }

            public void MonitorOpenProjectSystem(IIdeScope ideScope)
            {
            }

            public void MonitorOpenProject(ProjectSettings settings, int? featureFileCount)
            {
            }

            public void MonitorOpenFeatureFile(ProjectSettings projectSettings)
            {
            }

            public void MonitorExtensionInstalled()
            {
            }

            public void MonitorExtensionUpgraded(string oldExtensionVersion)
            {
            }

            public void MonitorExtensionDaysOfUsage(int usageDays)
            {
            }

            public void MonitorParserParse(ProjectSettings settings, Dictionary<string, object> additionalProps = null)
            {
            }

            public void MonitorCommandCommentUncomment()
            {
            }

            public void MonitorCommandDefineSteps(CreateStepDefinitionsDialogResult action, int snippetCount)
            {
            }

            public void MonitorCommandFindStepDefinitionUsages(int usagesCount, bool isCancelled)
            {
            }

            public void MonitorCommandGoToStepDefinition(bool generateSnippet)
            {
            }

            public void MonitorCommandAutoFormatTable()
            {
            }

            public void MonitorCommandAutoFormatDocument(bool isSelectionFormatting)
            {
            }

            public void MonitorCommandAddFeatureFile(ProjectSettings projectSettings)
            {
            }

            public void MonitorCommandAddSpecFlowConfigFile(ProjectSettings projectSettings)
            {
            }

            public void MonitorCommandRenameStepExecuted(RenameStepCommandContext ctx)
            {
            }

            public void MonitorSpecFlowDiscovery(bool isFailed, string errorMessage, int stepDefinitionCount, ProjectSettings projectSettings)
            {
            }

            public void MonitorSpecFlowGeneration(bool isFailed, ProjectSettings projectSettings)
            {
            }

            public void MonitorError(Exception exception, bool? isFatal = null)
            {
            }

            public void MonitorProjectTemplateWizardStarted()
            {
            }

            public void MonitorProjectTemplateWizardCompleted(string dotNetFramework, string unitTestFramework, bool addFluentAssertions)
            {
            }

            public void MonitorNotificationShown(NotificationData notification)
            {
            }

            public void MonitorNotificationDismissed(NotificationData notification)
            {
            }

            public void MonitorLinkClicked(string source, string url, Dictionary<string, object> additionalProps = null)
            {
            }

            public void MonitorUpgradeDialogDismissed(Dictionary<string, object> additionalProps)
            {
            }
        }

        public bool IsSolutionLoaded { get; } = false;

        public IDeveroomLogger Logger { get; }
        public IMonitoringService MonitoringService { get; }
        public IIdeActions Actions { get; }
        public IDeveroomWindowManager WindowManager { get; }
        public IFileSystem FileSystem { get; }
        public event EventHandler<EventArgs> WeakProjectsBuilt { add { } remove { } }
        public event EventHandler<EventArgs> WeakProjectOutputsUpdated { add { } remove { } }
        public IServiceProvider ServiceProvider { get; }
        public DTE Dte { get; }
        public IDeveroomOutputPaneServices DeveroomOutputPaneServices { get; }
        public IDeveroomErrorListServices DeveroomErrorListServices { get; }

        public IProjectScope GetProjectScope(Project project)
        {
            throw new NotImplementedException();
        }


        public NullVsIdeScope(IDeveroomLogger logger, IServiceProvider serviceProvider,
            IMonitoringService monitoringService)
        {
            Logger = logger;
            MonitoringService = monitoringService;
            ServiceProvider = serviceProvider;
            WindowManager = new DeveroomWindowManager(serviceProvider, monitoringService);
            FileSystem = new FileSystem();
            Actions = new NullIdeActions(this);
            Dte = null;
            DeveroomOutputPaneServices = null;
            DeveroomErrorListServices = null;
        }

        public void CalculateSourceLocationTrackingPositions(IEnumerable<SourceLocation> sourceLocations)
        {
        }

        public IProjectScope[] GetProjectsWithFeatureFiles()
        {
            return Array.Empty<IProjectScope>();
        }

        public IDisposable CreateUndoContext(string undoLabel)
        {
            return null;
        }

        public bool GetTextBuffer(SourceLocation sourceLocation, out ITextBuffer textBuffer)
        {
            textBuffer = default;
            return false;
        }

        public SyntaxTree GetSyntaxTree(ITextBuffer textBuffer)
        {
            return null;
        }

        public Task RunOnBackgroundThread(Func<Task> action, Action<Exception> onException)
        {
            return Task.CompletedTask;
        }

        public Task RunOnUiThread(Action action)
        {
            return Task.CompletedTask;
        }

        public void OpenIfNotOpened(string path)
        {
        }

        public IProjectScope GetProject(ITextBuffer textBuffer)
        {
            return null;
        }

        public void Dispose()
        {
            //nop
        }

        public static IMonitoringService GetNullMonitoringService()
        {
            return new NullMonitoringService();
        }
    }
}