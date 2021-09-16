#nullable enable
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
using SpecFlow.VisualStudio.Editor.Services.StepDefinitions;
using SpecFlow.VisualStudio.Editor.Services;
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
                var tb = IdeScope.GetTextBuffer(new SourceLocation(string.Empty, 1, 1));
  
            }            

            ctx.TextBufferOfStepDefinitionClass = textView.TextBuffer;
            var triggerPoint = textView.Caret.Position.BufferPosition;

            ValidateCallerProject(ctx);
            if (Erroneous(ctx)) return true;

            ValidateProjectsWithFeatureFiles(ctx);
            if (Erroneous(ctx)) return true;

            var stepDefinitions = CollectStepDefinitions(ctx, triggerPoint);

            PerformActions(stepDefinitions, ctx);
            return true;
        }

        private List<(IProjectScope specFlowTestProject, ProjectStepDefinitionBinding projectStepDefinitionBinding)> CollectStepDefinitions(RenameStepCommandContext ctx, SnapshotPoint triggerPoint)
        {
            var stepDefinitions =
                new List<(IProjectScope specFlowTestProject, ProjectStepDefinitionBinding projectStepDefinitionBinding)>();
            foreach (IProjectScope specFlowTestProject in ctx.SpecFlowTestProjectsWithFeatureFiles)
            {
                ProjectStepDefinitionBinding[] projectStepDefinitions = GetStepDefinitions(ctx, triggerPoint);
                foreach (var projectStepDefinitionBinding in projectStepDefinitions)
                {
                    stepDefinitions.Add((specFlowTestProject, projectStepDefinitionBinding));
                }
            }

            return stepDefinitions;
        }

        private void PerformActions(IReadOnlyList<(IProjectScope specFlowTestProject, ProjectStepDefinitionBinding projectStepDefinitionBinding)> stepDefinitions, RenameStepCommandContext ctx)
        {
            switch (stepDefinitions.Count)
            {
                case 0:
                    IdeScope.Actions.ShowProblem("No step definition found that is related to this position");
                    break;
                case 1:
                {
                    var selectedStepDefinition = stepDefinitions[0];
                    PerformActionsOnSelectedStepDefinition(selectedStepDefinition.specFlowTestProject,
                        selectedStepDefinition.projectStepDefinitionBinding, ctx);
                    break;
                }
                default:
                {
                    Logger.LogVerbose(
                        $"Choose step definitions from: {string.Join(", ", stepDefinitions.Select(sd => sd.projectStepDefinitionBinding.ToString()))}");
                    IdeScope.Actions.ShowSyncContextMenu(ChooseStepDefinitionPopupHeader, stepDefinitions.Select(sd =>
                        new ContextMenuItem(sd.projectStepDefinitionBinding.ToString(),
                            _ =>
                            {
                                PerformActionsOnSelectedStepDefinition(sd.specFlowTestProject, sd.projectStepDefinitionBinding,
                                    ctx);
                            },
                            "StepDefinitionsDefined")
                    ).ToArray());
                    break;
                }
            }
        }

        private void PerformActionsOnSelectedStepDefinition(IProjectScope stepDefinitionProjectScope, ProjectStepDefinitionBinding stepDefinitionBinding, RenameStepCommandContext ctx)
        {
            ctx.StepDefinitionBinding = stepDefinitionBinding;
            ctx.StepDefinitionProjectScope = stepDefinitionProjectScope;
            ExpressionIsValidAndSupported(ctx);
            if (Erroneous(ctx)) return;

            RenameStepViewModel viewModel = PrepareViewModel(ctx);
            //TODO: validate modified expression in the UI: the parameter expressions and order cannot be changed
            var result = IdeScope.WindowManager.ShowDialog(viewModel);
            if (result != true)
                return;

            viewModel.ParsedUpdatedExpression = ctx.StepDefinitionExpressionAnalyzer.Parse(viewModel.StepText);

            var validationErrors = Validate(ctx, viewModel.StepText);
            ctx.Issues.AddRange(validationErrors.Select(error=>new Issue(Issue.IssueKind.Problem, error)));
            if (Erroneous(ctx)) return;

            ctx.UpdatedExpression = viewModel.StepText;
            ctx.AnalyzedUpdatedExpression = viewModel.ParsedUpdatedExpression;
            //TODO: validate, although the form should have validated it anyway...

            _renameStepFeatureFileAction.PerformRenameStep(ctx);
            _renameStepStepDefinitionClassAction.PerformRenameStep(ctx);
            if (Erroneous(ctx)) return;
        }

        private bool Erroneous(RenameStepCommandContext ctx)
        {
            if (!ctx.IsErroneous) return false;

            foreach (var issue in ctx.Issues)
            {
                IdeScope.Actions.ShowProblem(issue.Description);
            }

            return true;
        }

        private void ValidateProjectsWithFeatureFiles(RenameStepCommandContext ctx)
        {
            ctx.SpecFlowTestProjectsWithFeatureFiles = IdeScope.GetProjectsWithFeatureFiles()
                .Where(p => p.GetProjectSettings().IsSpecFlowTestProject)
                .ToArray();

            if (ctx.SpecFlowTestProjectsWithFeatureFiles.Length == 0)
            {
                ctx.AddProblem("Unable to find step definition usages: could not find any SpecFlow project with feature files.");
            }
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

        private void ExpressionIsValidAndSupported(RenameStepCommandContext ctx)
        {
            switch (ctx.StepDefinitionBinding.Expression)
            {
                case null:
                    ctx.AddProblem( "Unable to rename step, the step definition expression cannot be detected.");
                    return;
                case "":
                    ctx.AddProblem("Step definition expression is invalid");
                    return;
            }

            ctx.StepDefinitionExpressionAnalyzer = new RegexStepDefinitionExpressionAnalyzer();
            ctx.AnalyzedOriginalExpression = ctx.StepDefinitionExpressionAnalyzer.Parse(ctx.StepDefinitionBinding.Expression);

            if (!ctx.AnalyzedOriginalExpression.ContainsOnlySimpleText)
                ctx.AddProblem("The non-parameter parts cannot contain expression operators");
        }

        private static RenameStepViewModel PrepareViewModel(RenameStepCommandContext ctx)
        {
            var viewModel = new RenameStepViewModel(ctx.StepDefinitionBinding, updatedExpression => Validate(ctx,updatedExpression));

            return viewModel;
        }

        public static ImmutableHashSet<string> Validate(RenameStepCommandContext ctx, string updatedExpression)
        {
            var errors = new HashSet<string>();
            var parsedUpdatedExpression = ctx.StepDefinitionExpressionAnalyzer.Parse(updatedExpression);
            if (parsedUpdatedExpression.Parts.Length != ctx.AnalyzedOriginalExpression.Parts.Length) errors.Add("Parameter count mismatch");

            return errors.ToImmutableHashSet();
        }

        private ProjectStepDefinitionBinding[] GetStepDefinitions(RenameStepCommandContext ctx, SnapshotPoint triggerPoint)
        {
            var fileName = GetEditorDocumentPath(ctx.TextBufferOfStepDefinitionClass);
            var discoveryService = ctx.ProjectOfStepDefinitionClass.GetDiscoveryService();
            var bindingRegistry = discoveryService.GetBindingRegistry();
            if (bindingRegistry == null)
                Logger.LogWarning($"Unable to get step definitions from project '{ctx.ProjectOfStepDefinitionClass.ProjectName}', usages will not be found for this project.");
            return FindStepDefinitionCommand.GetStepDefinitions(fileName, triggerPoint, bindingRegistry);
        }
    }
}
