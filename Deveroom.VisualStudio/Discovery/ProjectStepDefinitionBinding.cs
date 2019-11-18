using System.Linq;
using System.Text.RegularExpressions;
using Deveroom.VisualStudio.Editor.Services;
using Deveroom.VisualStudio.Editor.Services.Parser;
using Gherkin.Ast;

namespace Deveroom.VisualStudio.Discovery
{
    public class ProjectStepDefinitionBinding
    {
        public bool IsValid { get; set; } = true;
        public ScenarioBlock StepDefinitionType { get; }
        public Regex Regex { get; }
        public Scope Scope { get; }
        public ProjectStepDefinitionImplementation Implementation { get; }

        public ProjectStepDefinitionBinding(ScenarioBlock stepDefinitionType, Regex regex, Scope scope, ProjectStepDefinitionImplementation implementation)
        {
            StepDefinitionType = stepDefinitionType;
            Regex = regex;
            Scope = scope;
            Implementation = implementation;
        }

        public MatchResultItem Match(Step step, IGherkinDocumentContext context, string stepText = null)
        {
            if (!IsValid || !(step is DeveroomGherkinStep deveroomGherkinStep))
                return null;
            if (deveroomGherkinStep.ScenarioBlock != StepDefinitionType)
                return null;
            stepText = stepText ?? step.Text;
            var match = Regex.Match(stepText);
            if (!match.Success)
                return null;

            //check scope
            if (Scope != null)
            {
                if (Scope.Tag != null && !Scope.Tag.Evaluate(context.GetTagNames()))
                    return null;
                if (Scope.FeatureTitle != null && context.AncestorOrSelfNode<Feature>()?.Name != Scope.FeatureTitle)
                    return null;
                if (Scope.ScenarioTitle != null && context.AncestorOrSelfNode<Scenario>()?.Name != Scope.ScenarioTitle)
                    return null;
            }

            var parameterMatch = MatchParameter(step, match);
            return MatchResultItem.CreateMatch(this, parameterMatch);
        }

        private ParameterMatch MatchParameter(Step step, Match match)
        {
            var parameterCount = Implementation.ParameterTypes?.Length ?? 0;
            //if (match.Groups.Count == 1 && parameterCount == 0 && step.Argument == null)
            //    return ParameterMatch.Empty;
            var matchedStepParameters = match.Groups.OfType<Group>().Skip(1).Select(g => new MatchedStepTextParameter(g.Index, g.Length)).ToArray();
            var expectedParameterCount = matchedStepParameters.Length + (step.Argument == null ? 0 : 1);
            if (parameterCount != expectedParameterCount) //handle parameter error
                return new ParameterMatch(matchedStepParameters, step.Argument, Implementation.ParameterTypes,
                    $"The method '{Implementation.Method}' has invalid parameter count, {expectedParameterCount} parameter(s) expected");
            return new ParameterMatch(matchedStepParameters, step.Argument, Implementation.ParameterTypes);
        }

        public override string ToString()
        {
            return $"[{StepDefinitionType}({Regex})]: {Implementation}";
        }
    }
}

