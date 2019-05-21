using System;
using System.Linq;
using Deveroom.VisualStudio.Diagonostics;
using Deveroom.VisualStudio.Discovery;
using Deveroom.VisualStudio.ProjectSystem;
using Deveroom.VisualStudio.ProjectSystem.Configuration;
using Deveroom.VisualStudio.Snippets.Fallback;

namespace Deveroom.VisualStudio.Snippets
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

        public string GetStepDefinitionSkeletonSnippet(UndefinedStepDescriptor undefinedStep, string indent = "    ", string newLine = null)
        {
            try
            {
                var configuration = _projectScope.GetDeveroomConfiguration();
                newLine = newLine ?? Environment.NewLine;
                var result = FallbackStepDefinitionSkeletonProvider.GetStepDefinitionSkeletonSnippetFallback(undefinedStep, indent, newLine, configuration.BindingCulture);
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
