using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using SpecFlow.VisualStudio.Diagnostics;
using SpecFlow.VisualStudio.Discovery;
using SpecFlow.VisualStudio.Editor.Commands.Infrastructure;
using SpecFlow.VisualStudio.Editor.Services;
using SpecFlow.VisualStudio.Monitoring;
using SpecFlow.VisualStudio.ProjectSystem;
using SpecFlow.VisualStudio.ProjectSystem.Configuration;
using SpecFlow.VisualStudio.ProjectSystem.Settings;
using SpecFlow.VisualStudio.UI.ViewModels;

namespace SpecFlow.VisualStudio.Editor.Commands
{
    [Export(typeof(IDeveroomCodeEditorCommand))]
    [Export(typeof(IDeveroomFeatureEditorCommand))]
    public class RenameStepCommand : DeveroomEditorCommandBase, IDeveroomCodeEditorCommand, IDeveroomFeatureEditorCommand
    {
        private readonly StepDefinitionUsageFinder _stepDefinitionUsageFinder;
        
        [ImportingConstructor]
        public RenameStepCommand(IIdeScope ideScope, IBufferTagAggregatorFactoryService aggregatorFactory, IMonitoringService monitoringService) :
            base(ideScope, aggregatorFactory, monitoringService)
        {
            _stepDefinitionUsageFinder = new StepDefinitionUsageFinder(ideScope.FileSystem, ideScope.Logger, ideScope.MonitoringService);
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
                //TODO: Let the customer select the stepDefinition when there are more than one
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

            var viewModel = PrepareViewModel(stepDefinitionBinding);
            var result = IdeScope.WindowManager.ShowDialog(viewModel);
            if (result != true)
                return true;

            PerformRenameStepInFeatureFiles(selectedStepDefinition.Item1, stepDefinitionBinding, viewModel);
            PerformRenameStepInStepDefinitionClass(selectedStepDefinition.Item1, stepDefinitionBinding, textView, viewModel);

            return true;
        }

        private static RenameStepViewModel PrepareViewModel(ProjectStepDefinitionBinding stepDefinitionBinding)
        {
            var expression = stepDefinitionBinding.Expression.TrimStart('^').TrimEnd('$');

            var viewModel = new RenameStepViewModel(expression);

            return viewModel;
        }

        private void PerformRenameStepInFeatureFiles(IProjectScope projectScope, ProjectStepDefinitionBinding projectStepDefinitionBinding, RenameStepViewModel viewModel)
        {
            var featureFiles = projectScope.GetProjectFiles(".feature");
            var configuration = projectScope.GetDeveroomConfiguration();
            var projectUsages = _stepDefinitionUsageFinder.FindUsages(new[] {projectStepDefinitionBinding}, featureFiles, configuration);
            foreach (var fileUsage in projectUsages.GroupBy(u => u.SourceLocation.SourceFile))
            {
                var firstPosition = fileUsage.First().SourceLocation;
                EnsureFeatureFileOpen(firstPosition);
                var textBuffer = IdeScope.GetTextBuffer(firstPosition);
                EditTextBuffer(textBuffer, fileUsage, 
                    usage=>CalculateReplaceSpan((textBuffer, usage)), 
                    viewModel.StepText);
            }
        }

        private void EnsureFeatureFileOpen(SourceLocation sourceLocation)
        {
            IdeScope.Actions.NavigateTo(sourceLocation);
        }

        private static Span CalculateReplaceSpan((ITextBuffer textBuffer, StepDefinitionUsage usage) from)
        {
            var line = from.textBuffer.CurrentSnapshot.GetLineFromLineNumber(from.usage.SourceLocation.SourceFileLine - 1);
            var indentPlusKeywordLength = (from.usage.SourceLocation.SourceFileColumn - 1) + from.usage.Step.Keyword.Length;
            var startPosition = line.Start.Position + indentPlusKeywordLength;
            var replaceSpan = new Span(startPosition, line.End.Position - startPosition);
            return replaceSpan;
        }

        private void PerformRenameStepInStepDefinitionClass(IProjectScope projectScope, ProjectStepDefinitionBinding projectStepDefinitionBinding, IWpfTextView textView, RenameStepViewModel viewModel)
        {
            MethodDeclarationSyntax method = GetMethod(projectStepDefinitionBinding, textView.TextBuffer);

            IOrderedEnumerable<SyntaxToken> expressionsToReplace = ExpressionsToReplace(viewModel, method);

            EditTextBuffer(textView.TextBuffer, expressionsToReplace, CalculateReplaceSpan, viewModel.StepText);

            Logger.Log(TraceLevel.Info, method.AttributeLists.Count.ToString());
        }

        private static Span CalculateReplaceSpan(SyntaxToken token)
        {
            var offset = token.Text.IndexOf(token.ValueText, StringComparison.Ordinal);
            var trimLength = token.Text.Length - token.ValueText.Length;

            var replaceSpan = new Span(token.SpanStart + offset, token.Span.Length - trimLength);
            return replaceSpan;
        }

        private static MethodDeclarationSyntax GetMethod(ProjectStepDefinitionBinding projectStepDefinitionBinding,
            ITextBuffer textView)
        {
            Document roslynDocument = textView.GetRelatedDocuments().Single();
            var rootNode = roslynDocument.GetSyntaxRootAsync().Result;
            var methodLine =
                textView.CurrentSnapshot.GetLineFromLineNumber(projectStepDefinitionBinding.Implementation
                    .SourceLocation.SourceFileLine - 1);
            var methodColumn = projectStepDefinitionBinding.Implementation.SourceLocation.SourceFileColumn - 1;
            var methodPosition = methodLine.Start + methodColumn;
            var node = rootNode.FindNode(new TextSpan(methodPosition, 1));
            var method = node.Parent as MethodDeclarationSyntax;
            return method;
        }

        private static IOrderedEnumerable<SyntaxToken> ExpressionsToReplace(RenameStepViewModel viewModel, MethodDeclarationSyntax method)
        {
            var stepDefinitionAttributeTextTokens = method
                .AttributeLists
                .Select(al => al.Attributes.Single().ArgumentList.Arguments.Single().Expression.GetFirstToken())
                .Where(tok => tok.ValueText == viewModel.OriginalStepText)
                .OrderByDescending(tok => tok.SpanStart);
            return stepDefinitionAttributeTextTokens;
        }

        private static void EditTextBuffer<T>(
            ITextBuffer textBuffer,
            IEnumerable<T> expressionsToReplace,
            Func<T, Span> calculateReplaceSpan,
            string replacementText)
        {
            using var textEdit = textBuffer.CreateEdit();

            foreach (var token in expressionsToReplace)
            {
                var replaceSpan = calculateReplaceSpan(token);
                textEdit.Replace(replaceSpan, replacementText);
            }

            textEdit.Apply();
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
