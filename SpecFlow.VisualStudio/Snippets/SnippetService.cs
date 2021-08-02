using System;
using System.Linq;
using SpecFlow.VisualStudio.Diagonostics;
using SpecFlow.VisualStudio.Discovery;
using SpecFlow.VisualStudio.ProjectSystem;
using SpecFlow.VisualStudio.ProjectSystem.Configuration;
using SpecFlow.VisualStudio.ProjectSystem.Settings;
using SpecFlow.VisualStudio.Snippets.Fallback;

namespace SpecFlow.VisualStudio.Snippets
{
    public class SnippetService
    {
        private readonly IProjectScope _projectScope;
        private readonly IDeveroomLogger _logger;

        public SnippetService(IProjectScope projectScope)
        {
            _projectScope = projectScope;
            _logger = projectScope.IdeScope.Logger;
        }

        public SnippetExpressionStyle DefaultExpressionStyle =>
            _projectScope.GetProjectSettings().SpecFlowProjectTraits.HasFlag(SpecFlowProjectTraits.CucumberExpression)
                ? SnippetExpressionStyle.CucumberExpression
                : SnippetExpressionStyle.RegularExpression;

        public string GetStepDefinitionSkeletonSnippet(UndefinedStepDescriptor undefinedStep, SnippetExpressionStyle expressionStyle, string indent = "    ", string newLine = null)
        {
            try
            {
                var skeletonProvider = expressionStyle == SnippetExpressionStyle.CucumberExpression
                    ? (DeveroomStepDefinitionSkeletonProvider)new CucumberExpressionSkeletonProvider()
                    : new RegexStepDefinitionSkeletonProvider();

                var configuration = _projectScope.GetDeveroomConfiguration();
                newLine = newLine ?? Environment.NewLine;
                var result = skeletonProvider.GetStepDefinitionSkeletonSnippet(undefinedStep, indent, newLine, configuration.BindingCulture);
                _logger.LogInfo($"Step definition snippet generated for step '{undefinedStep.StepText}': {Environment.NewLine}{result}");
                return result;
            }
            catch(Exception e)
            {
                _projectScope.IdeScope.Actions.ShowError("Could not generate step definition snippet.", e);
                return "???";
            }
        }
    }
}
