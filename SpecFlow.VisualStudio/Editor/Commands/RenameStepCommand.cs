using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using SpecFlow.VisualStudio.Diagnostics;
using SpecFlow.VisualStudio.Discovery;
using SpecFlow.VisualStudio.Editor.Commands.Infrastructure;
using SpecFlow.VisualStudio.Monitoring;
using SpecFlow.VisualStudio.ProjectSystem;
using SpecFlow.VisualStudio.ProjectSystem.Actions;
using SpecFlow.VisualStudio.ProjectSystem.Settings;
using SpecFlow.VisualStudio.UI.ViewModels;

namespace SpecFlow.VisualStudio.Editor.Commands
{
    [Export(typeof(IDeveroomCodeEditorCommand))]
    [Export(typeof(IDeveroomFeatureEditorCommand))]
    public class RenameStepCommand : DeveroomEditorCommandBase, IDeveroomCodeEditorCommand, IDeveroomFeatureEditorCommand
    {
        const string ChooseStepDefinitionPopupHeader = "Choose step definition to rename";

        private RenameStepFeatureFileAction _renameStepFeatureFileAction;
        private RenameStepStepDefinitionClassAction _renameStepStepDefinitionClassAction;

        [ImportingConstructor]
        public RenameStepCommand(IIdeScope ideScope, IBufferTagAggregatorFactoryService aggregatorFactory, IMonitoringService monitoringService) :
            base(ideScope, aggregatorFactory, monitoringService)
        {
            _renameStepFeatureFileAction = new RenameStepFeatureFileAction();
            _renameStepStepDefinitionClassAction = new RenameStepStepDefinitionClassAction();
        }

        public override DeveroomEditorCommandTargetKey[] Targets => new[]
        {
            new DeveroomEditorCommandTargetKey(DeveroomCommands.DefaultCommandSet, DeveroomCommands.RenameStepCommandId)
        };

        public override bool PreExec(IWpfTextView textView, DeveroomEditorCommandTargetKey commandKey, IntPtr inArgs = default(IntPtr))
        {
            Logger.LogVerbose("Rename Step");

            var textBuffer = textView.TextBuffer;
            var fileName = GetEditorDocumentPath(textView);
            var triggerPoint = textView.Caret.Position.BufferPosition;

            if (ValidateCallerProject(textBuffer, out var project)) return true;

            if (ValidateProjectsWithFeatureFiles(out var specFlowTestProjects)) return true;

            var stepDefinitions = new List<Tuple<IProjectScope, ProjectStepDefinitionBinding>>();
            foreach (var specFlowTestProject in specFlowTestProjects)
            {
                var projectStepDefinitions = GetStepDefinitions(project, fileName, triggerPoint);
                foreach (var projectStepDefinitionBinding in projectStepDefinitions)
                {
                    stepDefinitions.Add(new Tuple<IProjectScope, ProjectStepDefinitionBinding>(specFlowTestProject, projectStepDefinitionBinding));
                }
            }

            if (stepDefinitions.Count == 0)
            {
                IdeScope.Actions.ShowProblem("No step definition found that is related to this position");
                return true;
            }

            if (stepDefinitions.Count != 1)
            {
                Logger.LogVerbose($"Choose step definitions from: {string.Join(", ", stepDefinitions.Select(sd => sd.Item2.ToString()))}");
                IdeScope.Actions.ShowSyncContextMenu(ChooseStepDefinitionPopupHeader, stepDefinitions.Select(sd =>
                    new ContextMenuItem(sd.Item2.ToString(), _ => { PerformRenameStepForStepDefinition(sd, textBuffer); }, "StepDefinitionsDefined")
                ).ToArray());
                return true;
            }

            PerformRenameStepForStepDefinition(stepDefinitions[0], textBuffer);
            return true;
        }

        private void PerformRenameStepForStepDefinition(Tuple<IProjectScope, ProjectStepDefinitionBinding> selectedStepDefinition, ITextBuffer textBuffer)
        {
            var stepDefinitionBinding = selectedStepDefinition.Item2;
            if (!ExpressionIsValidAndSupported(stepDefinitionBinding)) return;

            RenameStepViewModel viewModel = PrepareViewModel(selectedStepDefinition.Item1, stepDefinitionBinding);
            var result = IdeScope.WindowManager.ShowDialog(viewModel);
            if (result != true)
                return;

            _renameStepFeatureFileAction.PerformRenameStep(viewModel, textBuffer);
            if (!_renameStepStepDefinitionClassAction.PerformRenameStep(viewModel, textBuffer))
            {
                IdeScope.Actions.ShowProblem($"There was an error during step definition class rename. {viewModel.SelectedStepDefinitionBinding.Implementation.SourceLocation.SourceFile} not modified.");
            }
        }

        private bool ValidateProjectsWithFeatureFiles(out IProjectScope[] specFlowTestProjects)
        {
            specFlowTestProjects = IdeScope.GetProjectsWithFeatureFiles()
                .Where(p => p.GetProjectSettings().IsSpecFlowTestProject)
                .ToArray();

            if (specFlowTestProjects.Length == 0)
            {
                IdeScope.Actions.ShowProblem(
                    "Unable to find step definition usages: could not find any SpecFlow project with feature files.");
                return true;
            }

            return false;
        }

        private bool ValidateCallerProject(ITextBuffer textBuffer, out IProjectScope project)
        {
            project = IdeScope.GetProject(textBuffer);
            if (project == null || !project.GetProjectSettings().IsSpecFlowProject)
            {
                IdeScope.Actions.ShowProblem(
                    "Unable to find step definition usages: the project is not detected to be a SpecFlow project or it is not initialized yet.");
                return true;
            }

            return false;
        }

        private bool ExpressionIsValidAndSupported(ProjectStepDefinitionBinding stepDefinitionBinding)
        {
            if (stepDefinitionBinding.Expression is null)
            {
                IdeScope.Actions.ShowProblem("Unable to rename step, the step definition expression cannot be detected.");
                return false;
            }

            if (stepDefinitionBinding.Expression == string.Empty)
            {
                IdeScope.Actions.ShowProblem("Step definition expression is invalid");
                return false;
            }

            return true;
        }

        private static RenameStepViewModel PrepareViewModel(IProjectScope selectedStepDefinitionProject,
            ProjectStepDefinitionBinding stepDefinitionBinding)
        {
            var expression = stepDefinitionBinding.Expression.TrimStart('^').TrimEnd('$');

            var viewModel = new RenameStepViewModel(expression, selectedStepDefinitionProject, stepDefinitionBinding);

            return viewModel;
        }

        private ProjectStepDefinitionBinding[] GetStepDefinitions(IProjectScope project, string fileName, SnapshotPoint triggerPoint)
        {
            var discoveryService = project.GetDiscoveryService();
            var bindingRegistry = discoveryService.GetBindingRegistry();
            if (bindingRegistry == null)
                Logger.LogWarning($"Unable to get step definitions from project '{project.ProjectName}', usages will not be found for this project.");
            return FindStepDefinitionCommand.GetStepDefinitions(fileName, triggerPoint, bindingRegistry);
        }
    }
}
