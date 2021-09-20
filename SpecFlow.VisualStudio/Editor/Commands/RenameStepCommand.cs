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
        private const string NonParameterPartsCannotContainExpressionOperators = "The non-parameter parts cannot contain expression operators";

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

            if (textView.TextBuffer.ContentType.IsOfType(VsContentTypes.FeatureFile))
            {
                var goToStepDefinitionCommand = new GoToStepDefinitionCommand(IdeScope, AggregatorFactory, MonitoringService);
                goToStepDefinitionCommand.InvokeCommand(textView, sourceLocation =>
                {
                    var stepDefClassTextBuffer = IdeScope.GetTextBuffer(sourceLocation);
                    var stepDefLine = stepDefClassTextBuffer.CurrentSnapshot.GetLineFromLineNumber(sourceLocation.SourceFileLine - 1);
                    var stepDefinitionPosition = new SnapshotPoint(stepDefClassTextBuffer.CurrentSnapshot,
                        stepDefLine.Start.Position + sourceLocation.SourceFileColumn - 1);
                    InvokeCommandFromStepDefinitionClass(stepDefClassTextBuffer, stepDefinitionPosition);
                });
                return true;
            }

            var triggerPoint = textView.Caret.Position.BufferPosition;
            InvokeCommandFromStepDefinitionClass(textView.TextBuffer, triggerPoint);
            return true;
        }

        private void InvokeCommandFromStepDefinitionClass(ITextBuffer textBuffer, SnapshotPoint triggerPoint)
        {
            var ctx = new RenameStepCommandContext(IdeScope);
            ctx.TextBufferOfStepDefinitionClass = textBuffer;

            ValidateCallerProject(ctx);
            if (Erroneous(ctx)) return;

            ValidateProjectsWithFeatureFiles(ctx);
            if (Erroneous(ctx)) return;

            var stepDefinitions = CollectStepDefinitions(ctx, triggerPoint);

            PerformActions(stepDefinitions, ctx);
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
            var result = IdeScope.WindowManager.ShowDialog(viewModel);
            if (result != true)
                return;

            viewModel.ParsedUpdatedExpression = ctx.StepDefinitionExpressionAnalyzer.Parse(viewModel.StepText);

            var validationErrors = Validate(ctx, viewModel.StepText);
            ctx.Issues.AddRange(validationErrors.Select(error=>new Problem(Problem.ProblemKind.Critical, error)));
            if (Erroneous(ctx)) return;

            ctx.UpdatedExpression = viewModel.StepText;
            ctx.AnalyzedUpdatedExpression = viewModel.ParsedUpdatedExpression;

            using (IdeScope.CreateUndoContext("Rename steps"))
            {
                _renameStepFeatureFileAction.PerformRenameStep(ctx);
                _renameStepStepDefinitionClassAction.PerformRenameStep(ctx);
            }

            IdeScope.Actions.NavigateTo(ctx.StepDefinitionBinding.Implementation.SourceLocation);
            NotifyUserAboutIssues(ctx);
        }

        private void NotifyUserAboutIssues(RenameStepCommandContext ctx)
        {
            if (!ctx.Issues.Any()) return;
            ShowProblem(ctx);
        }

        private bool Erroneous(RenameStepCommandContext ctx)
        {
            if (!ctx.IsErroneous) return false;
            ShowProblem(ctx);
            return true;
        }

        private void ShowProblem(RenameStepCommandContext ctx)
        {
            var problem = string.Join(Environment.NewLine, ctx.Issues.Select(issue => issue.Description));
            IdeScope.Actions.ShowProblem(problem);
        }

        private void ValidateProjectsWithFeatureFiles(RenameStepCommandContext ctx)
        {
            ctx.SpecFlowTestProjectsWithFeatureFiles = IdeScope.GetProjectsWithFeatureFiles()
                .Where(p => p.GetProjectSettings().IsSpecFlowTestProject)
                .ToArray();

            if (ctx.SpecFlowTestProjectsWithFeatureFiles.Length == 0)
            {
                ctx.AddCriticalProblem("Unable to find step definition usages: could not find any SpecFlow project with feature files.");
            }
        }

        private void ValidateCallerProject(RenameStepCommandContext ctx)
        {
            ctx.ProjectOfStepDefinitionClass = IdeScope.GetProject(ctx.TextBufferOfStepDefinitionClass);
            if (ctx.ProjectOfStepDefinitionClass == null)
            {
                ctx.AddCriticalProblem("Unable to find step definition usages: the project is not initialized yet.");
            }
            else if (!ctx.ProjectOfStepDefinitionClass.GetProjectSettings().IsSpecFlowProject)
            {
                ctx.AddCriticalProblem("Unable to find step definition usages: the project is not detected to be a SpecFlow project.");
            }
        }

        private void ExpressionIsValidAndSupported(RenameStepCommandContext ctx)
        {
            switch (ctx.StepDefinitionBinding.Expression)
            {
                case null:
                    ctx.AddCriticalProblem( "Unable to rename step, the step definition expression cannot be detected.");
                    return;
                case "":
                    ctx.AddCriticalProblem("Step definition expression is invalid");
                    return;
            }

            ctx.StepDefinitionExpressionAnalyzer = new RegexStepDefinitionExpressionAnalyzer();
            ctx.AnalyzedOriginalExpression = ctx.StepDefinitionExpressionAnalyzer.Parse(ctx.StepDefinitionBinding.Expression);

            if (!ctx.AnalyzedOriginalExpression.ContainsOnlySimpleText)
                ctx.AddCriticalProblem(NonParameterPartsCannotContainExpressionOperators);
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

            if (ctx.AnalyzedOriginalExpression.ParameterParts
                .Zip(parsedUpdatedExpression.ParameterParts, (original, updated) => original == updated)
                .Any(eq => !eq))
            {
                errors.Add("Parameter expression mismatch");
            }

            if (!parsedUpdatedExpression.ContainsOnlySimpleText)
            {
                errors.Add(NonParameterPartsCannotContainExpressionOperators);
            }


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
