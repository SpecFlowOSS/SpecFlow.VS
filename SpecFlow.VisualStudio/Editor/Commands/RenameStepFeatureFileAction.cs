using System.Linq;
using Microsoft.VisualStudio.Text;
using SpecFlow.VisualStudio.Discovery;
using SpecFlow.VisualStudio.Editor.Services;
using SpecFlow.VisualStudio.ProjectSystem;
using SpecFlow.VisualStudio.ProjectSystem.Configuration;
using SpecFlow.VisualStudio.UI.ViewModels;

namespace SpecFlow.VisualStudio.Editor.Commands
{
    internal class RenameStepFeatureFileAction : RenameStepAction
    {
        public override bool PerformRenameStep(RenameStepViewModel viewModel,
            ITextBuffer textBufferOfStepDefinitionClass)
        {
            IIdeScope ideScope = viewModel.SelectedStepDefinitionProject.IdeScope;
            var stepDefinitionUsageFinder = new StepDefinitionUsageFinder(ideScope.FileSystem, ideScope.Logger, ideScope.MonitoringService);
            var featureFiles = viewModel.SelectedStepDefinitionProject.GetProjectFiles(".feature");
            var configuration = viewModel.SelectedStepDefinitionProject.GetDeveroomConfiguration();
            var projectUsages = stepDefinitionUsageFinder.FindUsages(new[] { viewModel.SelectedStepDefinitionBinding }, featureFiles, configuration);
            foreach (var fileUsage in projectUsages.GroupBy(u => u.SourceLocation.SourceFile))
            {
                var firstPosition = fileUsage.First().SourceLocation;
                EnsureFeatureFileOpen(firstPosition, ideScope);
                var textBufferOfFeatureFile = ideScope.GetTextBuffer(firstPosition);
                EditTextBuffer(textBufferOfFeatureFile, fileUsage,
                    usage => CalculateReplaceSpan((textBufferOfFeatureFile, usage)),
                    viewModel.StepText);
            }

            return true;
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