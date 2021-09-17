using System;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using SpecFlow.VisualStudio.Diagnostics;
using SpecFlow.VisualStudio.Discovery;
using SpecFlow.VisualStudio.Editor.Commands.Infrastructure;
using SpecFlow.VisualStudio.Editor.Services;
using SpecFlow.VisualStudio.Monitoring;
using SpecFlow.VisualStudio.ProjectSystem;
using SpecFlow.VisualStudio.ProjectSystem.Actions;
using SpecFlow.VisualStudio.ProjectSystem.Configuration;
using SpecFlow.VisualStudio.ProjectSystem.Settings;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;

namespace SpecFlow.VisualStudio.Editor.Commands
{
    [Export(typeof(IDeveroomCodeEditorCommand))]
    public class FindStepDefinitionCommand : DeveroomEditorCommandBase, IDeveroomCodeEditorCommand
    {
        const string PopupHeader = "Step definition usages";

        private readonly StepDefinitionUsageFinder _stepDefinitionUsageFinder;

        [ImportingConstructor]
        public FindStepDefinitionCommand(IIdeScope ideScope, IBufferTagAggregatorFactoryService aggregatorFactory, IMonitoringService monitoringService) : base(ideScope, aggregatorFactory, monitoringService)
        {
            _stepDefinitionUsageFinder = new StepDefinitionUsageFinder(ideScope.FileSystem, ideScope.Logger, ideScope.MonitoringService);
        }

        public override DeveroomEditorCommandTargetKey[] Targets => new[]
        {
            new DeveroomEditorCommandTargetKey(DeveroomCommands.DefaultCommandSet, DeveroomCommands.FindStepDefinitionUsagesCommandId),
        };

        public override DeveroomEditorCommandStatus QueryStatus(IWpfTextView textView, DeveroomEditorCommandTargetKey commandKey)
        {
            var status = base.QueryStatus(textView, commandKey);

            if (status != DeveroomEditorCommandStatus.NotSupported)
            {
                // very basic heuristic: if the word "SpecFlow" is in the content of the file, it might be a binding class
                status = textView.TextBuffer.CurrentSnapshot.GetText().Contains("SpecFlow")
                    ? DeveroomEditorCommandStatus.Supported
                    : DeveroomEditorCommandStatus.NotSupported;
            }

            return status;
        }

        public override bool PreExec(IWpfTextView textView, DeveroomEditorCommandTargetKey commandKey, IntPtr inArgs = default(IntPtr))
        {
            Logger.LogVerbose("Find Step Definition Usages");

            var textBuffer = textView.TextBuffer;
            var fileName = GetEditorDocumentPath(textBuffer);
            var triggerPoint = textView.Caret.Position.BufferPosition;

            var project = IdeScope.GetProject(textBuffer);
            if (project == null || !project.GetProjectSettings().IsSpecFlowProject)
            {
                IdeScope.Actions.ShowProblem("Unable to find step definition usages: the project is not detected to be a SpecFlow project or it is not initialized yet.");
                return true;
            }

            var specFlowTestProjects = IdeScope.GetProjectsWithFeatureFiles()
                .Where(p => p.GetProjectSettings().IsSpecFlowTestProject)
                .ToArray();

            if (specFlowTestProjects.Length == 0)
            {
                IdeScope.Actions.ShowProblem("Unable to find step definition usages: could not find any SpecFlow project with feature files.");
                return true;
            }

            var asyncContextMenu = IdeScope.Actions.ShowAsyncContextMenu(PopupHeader);
            Task.Run(() => FindUsagesInProjectsAsync(specFlowTestProjects, fileName, triggerPoint, asyncContextMenu, asyncContextMenu.CancellationToken), asyncContextMenu.CancellationToken);
            return true;
        }

        class FindUsagesSummary
        {
            public int FoundStepDefinitions { get; set; } = 0;
            public int ScannedFeatureFiles { get; set; } = 0;
            public int UsagesFound { get; set; } = 0;
            public bool WasError { get; set; } = false;
        }

        private async Task FindUsagesInProjectsAsync(IProjectScope[] specFlowTestProjects, string fileName, SnapshotPoint triggerPoint, IAsyncContextMenu asyncContextMenu, CancellationToken cancellationToken)
        {
            var summary = new FindUsagesSummary();

            try
            {
                await FindUsagesInternalAsync(specFlowTestProjects, fileName, triggerPoint, asyncContextMenu, 
                    cancellationToken, summary);
            }
            catch (Exception ex)
            {
                Logger.LogException(MonitoringService, ex);
                summary.WasError = true;
            }

            if (summary.WasError)
            {
                asyncContextMenu.AddItems(new ContextMenuItem("Could not complete find operation because of an error"));
            }
            else if (summary.FoundStepDefinitions == 0)
            {
                asyncContextMenu.AddItems(new ContextMenuItem("Could not find any step definitions at the current position"));
            }
            else if (summary.UsagesFound == 0)
            {
                asyncContextMenu.AddItems(new ContextMenuItem("Could not find any usage"));
            }

            MonitoringService.MonitorCommandFindStepDefinitionUsages(summary.UsagesFound, cancellationToken.IsCancellationRequested);
            if (cancellationToken.IsCancellationRequested)
                Logger.LogVerbose("Finding step definition usages cancelled");
            else
                Logger.LogInfo($"Found {summary.UsagesFound} usages in {summary.ScannedFeatureFiles} feature files");
            asyncContextMenu.Complete();
        }

        private async Task FindUsagesInternalAsync(IProjectScope[] specFlowTestProjects, string fileName, SnapshotPoint triggerPoint, IAsyncContextMenu asyncContextMenu, CancellationToken cancellationToken, FindUsagesSummary summary)
        {
            foreach (var project in specFlowTestProjects)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                var stepDefinitions = await GetStepDefinitionsAsync(project, fileName, triggerPoint);
                summary.FoundStepDefinitions += stepDefinitions.Length;
                if (stepDefinitions.Length == 0)
                    continue;

                var featureFiles = project.GetProjectFiles(".feature");
                var configuration = project.GetDeveroomConfiguration();
                var projectUsages = _stepDefinitionUsageFinder.FindUsages(stepDefinitions, featureFiles, configuration);
                foreach (var usage in projectUsages)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    //await Task.Delay(500);
                    asyncContextMenu.AddItems(CreateMenuItem(usage, project));
                    summary.UsagesFound++;
                }

                summary.ScannedFeatureFiles += featureFiles.Length;
            }
        }

        private ContextMenuItem CreateMenuItem(StepDefinitionUsage usage, IProjectScope project)
        {
            return new SourceLocationContextMenuItem(
                usage.SourceLocation, project.ProjectFolder, 
                GetUsageLabel(usage), _ => { PerformJump(usage); }, GetIcon());
        }

        private async Task<ProjectStepDefinitionBinding[]> GetStepDefinitionsAsync(IProjectScope project, string fileName, SnapshotPoint triggerPoint)
        {
            var discoveryService = project.GetDiscoveryService();
            var bindingRegistry = await discoveryService.GetBindingRegistryAsync();
            if (bindingRegistry == null)
                Logger.LogWarning($"Unable to get step definitions from project '{project.ProjectName}', usages will not be found for this project.");
            return GetStepDefinitions(fileName, triggerPoint, bindingRegistry);
        }

        internal static ProjectStepDefinitionBinding[] GetStepDefinitions(string fileName, SnapshotPoint triggerPoint, ProjectBindingRegistry bindingRegistry)
        {
            if (bindingRegistry == null)
                return Array.Empty<ProjectStepDefinitionBinding>();

            return bindingRegistry.StepDefinitions
                .Where(sd => sd.Implementation?.SourceLocation != null &&
                             sd.Implementation.SourceLocation.SourceFile == fileName &&
                             IsTriggerPointInStepDefinition(sd, triggerPoint))
                .ToArray();
        }

        private static bool IsTriggerPointInStepDefinition(ProjectStepDefinitionBinding stepDefinition, SnapshotPoint triggerPoint)
        {
            var sourceLocation = stepDefinition.Implementation.SourceLocation;
            var span = sourceLocation.SourceLocationSpan?.Span;
            if (span != null)
            {
                return IsTriggerPointInStepDefinition(span, triggerPoint);
            }

            //backup solution: we accept it 3 lines before the start line and 3 lines after
            // the start line if there is no end line, otherwise until the end line
            var triggerPointLine = triggerPoint.GetContainingLine().LineNumber + 1;
            if (triggerPointLine < sourceLocation.SourceFileLine - 3)
                return false;
            if (sourceLocation.HasEndPosition)
                return triggerPointLine <= sourceLocation.SourceFileEndLine;
            return triggerPointLine <= sourceLocation.SourceFileLine + 3;
        }

        private static bool IsTriggerPointInStepDefinition(ITrackingSpan stepDefinitionSpan, SnapshotPoint triggerPoint)
        {
            var span = stepDefinitionSpan.GetSpan(triggerPoint.Snapshot);
            if (span.Contains(triggerPoint) || span.End == triggerPoint) // Contains is end-exclusive
                return true;
            if (triggerPoint > span.End)
                return triggerPoint.GetContainingLine().LineNumber == span.End.GetContainingLine().LineNumber;
            if (triggerPoint < span.Start)
                return IsTriggerPointWithinExtendedInStepDefinition(span, triggerPoint);
            return false;
        }

        private static bool IsTriggerPointWithinExtendedInStepDefinition(SnapshotSpan span, SnapshotPoint triggerPoint)
        {
            // The debug info infrastructure we use sets the start position to the opening { of the step definition method.
            // To be able to find the definitions also from the attributes, we need to extend the start position.
            // Heuristic: extend it backwards until a) first empty line b) a curly brace ({ or }) c) a semicolon.
            // Later we can improve this using Roslyn

            var triggerPointLine = triggerPoint.GetContainingLine();
            var stepDefStartLine = span.Start.GetContainingLine();
            if (stepDefStartLine.LineNumber == triggerPointLine.LineNumber)
                return true; // on the same line

            if (stepDefStartLine.LineNumber - triggerPointLine.LineNumber > 10)
                return false; // if more than 10 lines away, it is surely not the step definition

            while (stepDefStartLine.LineNumber > triggerPointLine.LineNumber)
            {
                stepDefStartLine = stepDefStartLine.Snapshot.GetLineFromLineNumber(stepDefStartLine.LineNumber - 1);
                var lineText = stepDefStartLine.GetText();
                if (string.IsNullOrWhiteSpace(lineText) || Regex.IsMatch(lineText, @"[;\{\}]"))
                    return false;
            }
            return true;
        }

        private string GetUsageLabel(StepDefinitionUsage usage)
        {
            return $"{usage.Step.Keyword}{usage.Step.Text}";
        }

        private string GetIcon()
        {
            return null;
        }

        private void PerformJump(StepDefinitionUsage usage)
        {
            var sourceLocation = usage.SourceLocation;
            if (sourceLocation == null)
            {
                Logger.LogWarning($"Cannot jump to {usage}: no source location");
                IdeScope.Actions.ShowProblem("Unable to jump to the step. No source location detected.");
                return;
            }

            Logger.LogInfo($"Jumping to {usage} at {sourceLocation}");
            if (!IdeScope.Actions.NavigateTo(sourceLocation))
            {
                Logger.LogWarning($"Cannot jump to {usage}: invalid source file or position");
                IdeScope.Actions.ShowProblem($"Unable to jump to the step. Invalid source file or file position.{Environment.NewLine}{sourceLocation}");
            }
        }
    }
}
