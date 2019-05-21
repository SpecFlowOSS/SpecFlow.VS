using System;
using System.Collections.Generic;
using System.Linq;
using Gherkin;
using Gherkin.Ast;

namespace Deveroom.VisualStudio.Editor.Services.Parser
{
    public static class GherkinAstExtensions
    {
        public static IEnumerable<StepsContainer> StepsContainers(this Feature feature)
            => feature.Children.OfType<StepsContainer>();

        public static IEnumerable<Scenario> ScenarioDefinitions(this Feature feature)
            => feature.Children.OfType<Scenario>();

        public static Background Background(this Feature feature)
            => feature.Children.OfType<Background>().FirstOrDefault();

        public static ScenarioBlock? ToScenarioBlock(this StepKeyword stepKeyword)
        {
            switch (stepKeyword)
            {
                case StepKeyword.Given:
                    return ScenarioBlock.Given;
                case StepKeyword.When:
                    return ScenarioBlock.When;
                case StepKeyword.Then:
                    return ScenarioBlock.Then;
            }
            return null;
        }

        public static string[] GetBlockKeywords(this GherkinDialect gherkinDialect)
        {
            return gherkinDialect.FeatureKeywords
                .Concat(gherkinDialect.BackgroundKeywords)
                .Concat(gherkinDialect.ScenarioKeywords)
                .Concat(gherkinDialect.ScenarioOutlineKeywords)
                .Concat(gherkinDialect.ExamplesKeywords)
                .ToArray();
        }
    }
}
