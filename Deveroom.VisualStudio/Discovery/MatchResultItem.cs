using System;
using System.Linq;
using Deveroom.VisualStudio.Editor.Services.Parser;
using Gherkin.Ast;

namespace Deveroom.VisualStudio.Discovery
{
    public class UndefinedStepDescriptor
    {
        public Step UndefinedStep { get; }
        public string CustomStepText { get; }

        public string StepText => CustomStepText ?? UndefinedStep.Text;
        public ScenarioBlock ScenarioBlock => ((DeveroomGherkinStep) UndefinedStep).ScenarioBlock;
        public bool HasDataTable => UndefinedStep.Argument is DataTable;
        public bool HasDocString => UndefinedStep.Argument is DocString;

        public UndefinedStepDescriptor(Step undefinedStep, string customStepText)
        {
            UndefinedStep = undefinedStep;
            CustomStepText = customStepText;
        }
    }

    public class MatchResultItem
    {
        private static readonly string[] EmptyErrors = new string[0];
        public MatchResultType Type { get; }

        // step definition match
        public ProjectStepDefinitionBinding MatchedStepDefinition { get; }
        public ParameterMatch ParameterMatch { get; }

        // undefined step
        public UndefinedStepDescriptor UndefinedStep { get; }

        public string[] Errors { get; }
        public bool HasErrors => Errors.Any();

        private MatchResultItem(MatchResultType type, ProjectStepDefinitionBinding matchedStepDefinition, ParameterMatch parameterMatch, string[] errors, UndefinedStepDescriptor undefinedStep)
        {
            Type = type;
            MatchedStepDefinition = matchedStepDefinition;
            ParameterMatch = parameterMatch;
            UndefinedStep = undefinedStep;
            Errors = errors ?? EmptyErrors;
        }

        public override string ToString()
        {
            switch (Type)
            {
                case MatchResultType.Undefined:
                    return "Undefined";
                case MatchResultType.Defined:
                    return $"Defined: {MatchedStepDefinition}";
                case MatchResultType.Ambiguous:
                    return $"Ambiguous: {MatchedStepDefinition}";
            }
            return "";
        }

        public MatchResultItem CloneToAmbiguousItem()
        {
            return new MatchResultItem(MatchResultType.Ambiguous, MatchedStepDefinition, ParameterMatch, Errors, null);
        }

        public static MatchResultItem CreateMatch(ProjectStepDefinitionBinding matchedStepDefinition, ParameterMatch parameterMatch)
        {
            if (matchedStepDefinition == null) throw new ArgumentNullException(nameof(matchedStepDefinition));
            if (parameterMatch == null) throw new ArgumentNullException(nameof(parameterMatch));

            string[] errors = null;
            if (parameterMatch.HasError)
                errors = new[] { parameterMatch.Error };

            return new MatchResultItem(MatchResultType.Defined,
                matchedStepDefinition, parameterMatch, errors, null);
        }

        public static MatchResultItem CreateUndefined(Step step, string customStepText)
        {
            return new MatchResultItem(MatchResultType.Undefined,
                null, null, null, new UndefinedStepDescriptor(step, customStepText));
        }
    }
}