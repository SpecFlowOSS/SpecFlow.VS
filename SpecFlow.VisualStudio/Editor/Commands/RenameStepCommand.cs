using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
using SpecFlow.VisualStudio.ProjectSystem.Settings;
using SpecFlow.VisualStudio.UI.ViewModels;

namespace SpecFlow.VisualStudio.Editor.Commands
{
    [Export(typeof(IDeveroomCodeEditorCommand))]
    [Export(typeof(IDeveroomFeatureEditorCommand))]
    public class RenameStepCommand : DeveroomEditorCommandBase, IDeveroomCodeEditorCommand, IDeveroomFeatureEditorCommand
    {
        private readonly ImmutableHashSet<IRenameStepPerform> _renameStepPerforms;
        
        [ImportingConstructor]
        public RenameStepCommand(IIdeScope ideScope, IBufferTagAggregatorFactoryService aggregatorFactory, IMonitoringService monitoringService) :
            base(ideScope, aggregatorFactory, monitoringService)
        {
            _renameStepPerforms = ImmutableHashSet.Create<IRenameStepPerform>(
                new RenameStepPerformInFeatureFiles(),
                new RenameStepPerformInStepDefinitionClass()
            );
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
                //TODO: 13793 Let the customer select the stepDefinition when there are more than one
                IdeScope.Actions.ShowProblem("TODO: multiple projects/stepdefs not supported");
                return true;
            }

            var selectedStepDefinition = stepDefinitions[0];
            var stepDefinitionBinding = selectedStepDefinition.Item2;
            if (stepDefinitionBinding.Expression == null)
            {
                IdeScope.Actions.ShowProblem("Unable to rename step, the step definition expression cannot be detected.");
                return true;
            }

            RenameStepViewModel viewModel = PrepareViewModel(selectedStepDefinition.Item1, stepDefinitionBinding);
            var result = IdeScope.WindowManager.ShowDialog(viewModel);
            if (result != true)
                return true;

            foreach (var renameStepPerform in _renameStepPerforms)
            { 
                renameStepPerform.PerformRenameStep(viewModel, textBuffer);
            }

            return true;
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
