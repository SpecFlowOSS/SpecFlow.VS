using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows.Documents;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using SpecFlow.VisualStudio.Diagnostics;
using SpecFlow.VisualStudio.Discovery;
using SpecFlow.VisualStudio.Editor.Commands.Infrastructure;
using SpecFlow.VisualStudio.Editor.Services.StepDefinitions;
using SpecFlow.VisualStudio.Editor.Services;
using SpecFlow.VisualStudio.Monitoring;
using SpecFlow.VisualStudio.ProjectSystem;
using SpecFlow.VisualStudio.ProjectSystem.Actions;
using SpecFlow.VisualStudio.ProjectSystem.Settings;
using SpecFlow.VisualStudio.UI.ViewModels;

namespace SpecFlow.VisualStudio.Editor.Commands
{

    public record Issue
    {
        public enum IssueKind
        {
            Problem
        }

        public Issue(IssueKind kind, string description)
        {
            Kind = kind;
            Description = description;
        }

        public IssueKind Kind { get; }
        public string Description { get; }

        public override string ToString()
        {
            return $"{Kind,10} {Description}";
        }
    }

    [Export(typeof(IDeveroomCodeEditorCommand))]
    [Export(typeof(IDeveroomFeatureEditorCommand))]
    public class RenameStepCommand : DeveroomEditorCommandBase, IDeveroomCodeEditorCommand, IDeveroomFeatureEditorCommand
    {
        const string ChooseStepDefinitionPopupHeader = "Choose step definition to rename";

        private readonly RenameStepFeatureFileAction _renameStepFeatureFileAction;
        private readonly RenameStepStepDefinitionClassAction _renameStepStepDefinitionClassAction;

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
            var ctx = new RenameStepCommandContext(IdeScope);

            var stepTag = GetDeveroomTagForCaret(textView, DeveroomTagTypes.StepBlock);
            if (stepTag != null)
            {
                var goToStepDefinitionCommand = new GoToStepDefinitionCommand(IdeScope, AggregatorFactory, MonitoringService);
                goToStepDefinitionCommand.PreExec(textView, commandKey, inArgs);
                var tb = IdeScope.GetTextBuffer(new SourceLocation(String.Empty, 1, 1));
  
            }            

            ctx.TextBufferOfStepDefinitionClass = textView.TextBuffer;
            
            var fileName = GetEditorDocumentPath(ctx.TextBufferOfStepDefinitionClass);
            var triggerPoint = textView.Caret.Position.BufferPosition;

            ValidateCallerProject(ctx);
            if (Erroneous(ctx)) return true;

            if (ValidateProjectsWithFeatureFiles(out var specFlowTestProjects)) return true;

            var stepDefinitions = new List<Tuple<IProjectScope, ProjectStepDefinitionBinding>>();
            foreach (var specFlowTestProject in specFlowTestProjects)
            {
                var projectStepDefinitions = GetStepDefinitions(ctx.ProjectOfStepDefinitionClass, fileName, triggerPoint);
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
                    new ContextMenuItem(sd.Item2.ToString(), _ => { PerformRenameStepForStepDefinition(sd, ctx); }, "StepDefinitionsDefined")
                ).ToArray());
                return true;
            }

            PerformRenameStepForStepDefinition(stepDefinitions[0], ctx);

            return true;
        }

        private void PerformRenameStepForStepDefinition(Tuple<IProjectScope, ProjectStepDefinitionBinding> selectedStepDefinition, RenameStepCommandContext ctx)
        {
            var stepDefinitionBinding = selectedStepDefinition.Item2;
            ExpressionIsValidAndSupported(stepDefinitionBinding, ctx);
            if (Erroneous(ctx)) return;

            RenameStepViewModel viewModel = PrepareViewModel(selectedStepDefinition.Item1, stepDefinitionBinding, ctx);
            //TODO: validate modified expression in the UI: the parameter expressions and order cannot be changed
            var result = IdeScope.WindowManager.ShowDialog(viewModel);
            if (result != true)
                return;

            viewModel.ParsedUpdatedExpression = viewModel.StepDefinitionExpressionAnalyzer.Parse(viewModel.StepText);
            //TODO: validate, although the form should have validated it anyway...

            _renameStepFeatureFileAction.PerformRenameStep(viewModel, ctx.TextBufferOfStepDefinitionClass);
            if (!_renameStepStepDefinitionClassAction.PerformRenameStep(viewModel, ctx.TextBufferOfStepDefinitionClass))
            {
                IdeScope.Actions.ShowProblem("There was an error during step definition class rename.");
            }
        }

        private bool Erroneous(RenameStepCommandContext ctx)
        {
            if (ctx.Issues.Count <= 0) return false;

            foreach (var issue in ctx.Issues)
            {
                IdeScope.Actions.ShowProblem(issue.Description);
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

        private void ValidateCallerProject(RenameStepCommandContext ctx)
        {
            ctx.ProjectOfStepDefinitionClass = IdeScope.GetProject(ctx.TextBufferOfStepDefinitionClass);
            if (ctx.ProjectOfStepDefinitionClass == null)
            {
                ctx.AddProblem("Unable to find step definition usages: the project is not initialized yet.");
            }
            else if (!ctx.ProjectOfStepDefinitionClass.GetProjectSettings().IsSpecFlowProject)
            {
                ctx.AddProblem("Unable to find step definition usages: the project is not detected to be a SpecFlow project.");
            }
        }

        private void ExpressionIsValidAndSupported(ProjectStepDefinitionBinding stepDefinitionBinding, RenameStepCommandContext ctx)
        {
            ctx.Analyzer = new RegexStepDefinitionExpressionAnalyzer();
            ctx.AnalyzedExpression = ctx.Analyzer.Parse(stepDefinitionBinding.Expression);

            if (!ctx.AnalyzedExpression.ContainsOnlySimpleText)
                ctx.AddProblem("The non-parameter parts cannot contain expression operators");

            switch (stepDefinitionBinding.Expression)
            {
                case null:
                    ctx.AddProblem( "Unable to rename step, the step definition expression cannot be detected.");
                    break;
                case "":
                    ctx.AddProblem("Step definition expression is invalid");
                    break;
            }
        }

        private static RenameStepViewModel PrepareViewModel(IProjectScope selectedStepDefinitionProject,
            ProjectStepDefinitionBinding stepDefinitionBinding, RenameStepCommandContext ctx)
        {
            var expression = stepDefinitionBinding.Expression.TrimStart('^').TrimEnd('$');

            var viewModel = new RenameStepViewModel(expression, selectedStepDefinitionProject, stepDefinitionBinding, ctx.AnalyzedExpression, ctx.Analyzer);

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
