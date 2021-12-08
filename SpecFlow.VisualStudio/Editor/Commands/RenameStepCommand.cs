#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
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
            new DeveroomEditorCommandTargetKey(SpecFlowVsCommands.DefaultCommandSet, SpecFlowVsCommands.RenameStepCommandId)
        };

        public override bool PreExec(IWpfTextView textView, DeveroomEditorCommandTargetKey commandKey, IntPtr inArgs = default(IntPtr))
        {
            Logger.LogVerbose("Rename Step");
            var ctx = new RenameStepCommandContext(IdeScope);

            if (textView.TextBuffer.ContentType.IsOfType(VsContentTypes.FeatureFile))
            {
                var goToStepDefinitionCommand = new GoToStepDefinitionCommand(IdeScope, AggregatorFactory, MonitoringService);
                goToStepDefinitionCommand.InvokeCommand(textView, projectStepDefinitionBinding =>
                {
                    ctx.StepDefinitionBinding = projectStepDefinitionBinding;
                    var sourceLocation = projectStepDefinitionBinding.Implementation.SourceLocation;
                    IdeScope.GetTextBuffer(sourceLocation, out var textBuffer);
                    ctx.TextBufferOfStepDefinitionClass = textBuffer;
                    var stepDefLine = ctx.TextBufferOfStepDefinitionClass.CurrentSnapshot.GetLineFromLineNumber(sourceLocation.SourceFileLine - 1);
                    ctx.TriggerPointOfStepDefinitionClass = new SnapshotPoint(ctx.TextBufferOfStepDefinitionClass.CurrentSnapshot,
                        stepDefLine.Start.Position + sourceLocation.SourceFileColumn - 1);
                    
                    InvokeCommandFromStepDefinitionClass(ctx);
                });
                return true;
            }

            ctx.TriggerPointOfStepDefinitionClass = textView.Caret.Position.BufferPosition;
            ctx.TextBufferOfStepDefinitionClass = textView.TextBuffer;
            InvokeCommandFromStepDefinitionClass(ctx);
            return true;
        }

        private void InvokeCommandFromStepDefinitionClass(RenameStepCommandContext ctx)
        {
            ValidateCallerProject(ctx);
            if (Erroneous(ctx)) return;

            ValidateProjectsWithFeatureFiles(ctx);
            if (Erroneous(ctx)) return;

            var stepDefinitions = CollectStepDefinitions(ctx);

            PerformActions(stepDefinitions, ctx);
        }

        private List<(IProjectScope specFlowTestProject, ProjectStepDefinitionBinding projectStepDefinitionBinding)> CollectStepDefinitions(RenameStepCommandContext ctx)
        {
            var stepDefinitions =
                new List<(IProjectScope specFlowTestProject, ProjectStepDefinitionBinding projectStepDefinitionBinding)>();
            foreach (IProjectScope specFlowTestProject in ctx.SpecFlowTestProjectsWithFeatureFiles)
            {
                ProjectStepDefinitionBinding[] projectStepDefinitions = GetStepDefinitions(ctx);
                foreach (var projectStepDefinitionBinding in projectStepDefinitions)
                {
                    if (ctx.StepDefinitionBinding == null) 
                        stepDefinitions.Add((specFlowTestProject, projectStepDefinitionBinding));

                    if (ctx.StepDefinitionBinding == projectStepDefinitionBinding)
                    {
                        stepDefinitions.Add((specFlowTestProject, projectStepDefinitionBinding));
                        return stepDefinitions;
                    }
                }
            }

            return stepDefinitions;
        }

        private void PerformActions(IReadOnlyList<(IProjectScope specFlowTestProject, ProjectStepDefinitionBinding projectStepDefinitionBinding)> stepDefinitions, RenameStepCommandContext ctx)
        {
            switch (stepDefinitions.Count)
            {
                case 0:
                    ctx.AddCriticalProblem("No step definition found that is related to this position");
                    NotifyUserAboutIssues(ctx);
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

            PerformModifications(ctx);
        }

        private void PerformModifications(RenameStepCommandContext ctx)
        {
            InvokeOnBackgroundThread(ctx, async () =>
            {
                using (IdeScope.CreateUndoContext("Rename steps"))
                {
                    await _renameStepFeatureFileAction.PerformRenameStep(ctx);
                    await _renameStepStepDefinitionClassAction.PerformRenameStep(ctx);
                }

                IdeScope.Actions.NavigateTo(ctx.StepDefinitionBinding.Implementation.SourceLocation);
                await UpdateBindingRegistry(ctx);
            });
        }

        private void InvokeOnBackgroundThread(RenameStepCommandContext ctx, Func<Task> action)
        {
            _ = ctx.IdeScope.RunOnBackgroundThread(action, e=> ctx.AddCriticalProblem(e.Message))
                .ContinueWith(_=>NotifyUserAboutIssues(ctx), TaskScheduler.Default);
        }

        private Task NotifyUserAboutIssues(RenameStepCommandContext ctx)
        {
            if (!ctx.Issues.Any())
            {
                MonitoringService.MonitorCommandRenameStepExecuted(ctx);
                Finished.Set();
                return Task.CompletedTask;
            }

            ShowProblem(ctx);
            return Task.CompletedTask;
        }

        private bool Erroneous(RenameStepCommandContext ctx)
        {
            if (!ctx.IsErroneous) return false;
            ShowProblem(ctx);
            return true;
        }

        private void ShowProblem(RenameStepCommandContext ctx)
        {
            var problems = string.Join(Environment.NewLine, ctx.Issues.Select(issue => issue.Description));
            IdeScope.Actions.ShowProblem(
                $"The following problems occurred:{Environment.NewLine}{problems}", "Rename Step");
            MonitoringService.MonitorCommandRenameStepExecuted(ctx);
            Finished.Set();
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

        private ProjectStepDefinitionBinding[] GetStepDefinitions(RenameStepCommandContext ctx)
        {
            var fileName = GetEditorDocumentPath(ctx.TextBufferOfStepDefinitionClass);
            var bindingRegistry = GetBindingRegistry(ctx);
            return FindStepDefinitionUsagesCommand.GetStepDefinitions(fileName, ctx.TriggerPointOfStepDefinitionClass, bindingRegistry);
        }

        private ProjectBindingRegistry GetBindingRegistry(RenameStepCommandContext ctx)
        {
            var discoveryService = ctx.ProjectOfStepDefinitionClass.GetDiscoveryService();
            var bindingRegistry = discoveryService.GetLastProcessedBindingRegistry();
            return bindingRegistry;
        }

        private Task UpdateBindingRegistry(RenameStepCommandContext ctx)
        {
            var discoveryService = ctx.ProjectOfStepDefinitionClass.GetDiscoveryService();
            return discoveryService.UpdateBindingRegistry(bindingRegistry =>
                bindingRegistry.ReplaceStepDefinition(ctx.StepDefinitionBinding, ctx.StepDefinitionBinding.WithSpecifiedExpression(ctx.UpdatedExpression)));

        }
    }
}
