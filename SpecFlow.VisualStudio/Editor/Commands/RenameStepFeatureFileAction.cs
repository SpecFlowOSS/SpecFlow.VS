using System.Linq;
using System.Text;
using Microsoft.VisualStudio.Text;
using SpecFlow.VisualStudio.Discovery;
using SpecFlow.VisualStudio.Editor.Commands.Infrastructure;
using SpecFlow.VisualStudio.Editor.Services;
using SpecFlow.VisualStudio.Editor.Services.StepDefinitions;
using SpecFlow.VisualStudio.ProjectSystem;
using SpecFlow.VisualStudio.ProjectSystem.Actions;
using SpecFlow.VisualStudio.ProjectSystem.Configuration;
using SpecFlow.VisualStudio.UI.ViewModels;

namespace SpecFlow.VisualStudio.Editor.Commands
{
    internal class RenameStepFeatureFileAction : RenameStepAction
    {
        public override void PerformRenameStep(RenameStepCommandContext ctx)
        {
            IIdeScope ideScope = ctx.ProjectOfStepDefinitionClass.IdeScope;
            var stepDefinitionUsageFinder = new StepDefinitionUsageFinder(ideScope.FileSystem, ideScope.Logger, ideScope.MonitoringService);
            var featureFiles = ctx.ProjectOfStepDefinitionClass.GetProjectFiles(".feature");
            var configuration = ctx.ProjectOfStepDefinitionClass.GetDeveroomConfiguration();
            var projectUsages = stepDefinitionUsageFinder.FindUsages(new[] { ctx.StepDefinitionBinding }, featureFiles, configuration).ToArray();
            foreach (var fileUsage in projectUsages.GroupBy(u => u.SourceLocation.SourceFile))
            {
                var firstPosition = fileUsage.First().SourceLocation;
                EnsureFeatureFileOpen(firstPosition, ideScope);
                var textBufferOfFeatureFile = ideScope.GetTextBuffer(firstPosition);
                EditTextBuffer(textBufferOfFeatureFile, fileUsage,
                    usage => CalculateReplaceSpan((textBufferOfFeatureFile, usage)),
                    usage => CalculateReplacementText((textBufferOfFeatureFile, usage), ctx.AnalyzedUpdatedExpression, fileUsage.Key, ctx));
            }
        }

        private string CalculateReplacementText((ITextBuffer textBufferOfFeatureFile, StepDefinitionUsage usage) @from,
            AnalyzedStepDefinitionExpression updatedExpression, string filePath,
            RenameStepCommandContext renameStepCommandContext)
        {
            //TODO: make a shortcut for simple expressions (no params), in that case we just need to return viewModel.StepText

            var snapshotSpan = new SnapshotSpan(from.textBufferOfFeatureFile.CurrentSnapshot, CalculateReplaceSpan(from));
            var matchedStepTag =
                DeveroomEditorCommandBase.GetDeveroomTagsForSpan(from.textBufferOfFeatureFile, snapshotSpan)
                    .FirstOrDefault(t => t.Type == DeveroomTagTypes.DefinedStep);

            var matchResult = matchedStepTag?.Data as MatchResult;
            var parameterMatch = matchResult?.Items
                .FirstOrDefault(m => m.ParameterMatch != null)
                ?.ParameterMatch;
            
            if (parameterMatch == null 
                || parameterMatch.StepTextParameters.Length != updatedExpression.ParameterParts.Count() 
                || matchedStepTag.ParentTag.GetDescendantsOfType(DeveroomTagTypes.ScenarioOutlinePlaceholder).Any())
            {
                //TODO: Calculate lina and column
                renameStepCommandContext.AddNotificationProblem($"{filePath}(12,12): Could not rename scenario outline step: {from.usage.Step.Text} ");
                return from.usage.Step.Text;
            }

            var resultText = new StringBuilder();
            var text = from.usage.Step.Text;
            for (int i = 0; i < parameterMatch.StepTextParameters.Length; i++)
            {
                resultText.Append(GetUnescapedText(updatedExpression.Parts[i * 2]));
                resultText.Append(text.Substring(parameterMatch.StepTextParameters[i].Index, parameterMatch.StepTextParameters[i].Length));
            }
            resultText.Append(GetUnescapedText(updatedExpression.Parts.Last()));

            return resultText.ToString();
        }

        private string GetUnescapedText(AnalyzedStepDefinitionExpressionPart part)
        {
            if (part is AnalyzedStepDefinitionExpressionSimpleTextPart simpleTextPart)
                return simpleTextPart.UnescapedText;
            return part.ExpressionText;
        }

        protected void EnsureFeatureFileOpen(SourceLocation sourceLocation, IIdeScope ideScope)
        {
            ideScope.Actions.NavigateTo(sourceLocation);
        }


        private static Span CalculateReplaceSpan((ITextBuffer textBuffer, StepDefinitionUsage usage) from)
        {
            var line = from.textBuffer.CurrentSnapshot.GetLineFromLineNumber(from.usage.SourceLocation.SourceFileLine - 1);
            var indentPlusKeywordLength = (from.usage.SourceLocation.SourceFileColumn - 1) + from.usage.Step.Keyword.Length;
            var startPosition = line.Start.Position + indentPlusKeywordLength;
            var replaceSpan = new Span(startPosition, line.End.Position - startPosition);
            return replaceSpan;
        }
    }
}