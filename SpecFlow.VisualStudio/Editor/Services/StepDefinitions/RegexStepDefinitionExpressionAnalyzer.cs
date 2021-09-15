using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using SpecFlow.VisualStudio.Discovery;

namespace SpecFlow.VisualStudio.Editor.Services.StepDefinitions
{
    public class RegexStepDefinitionExpressionAnalyzer : IStepDefinitionExpressionAnalyzer
    {
        public AnalyzedStepDefinitionExpression Parse(string expression)
        {
            var matches = Regex.Matches(expression, @"\([^\)]+\)"); //TODO: make proper split, see StepDefinitionSampler
            var parts = new List<AnalyzedStepDefinitionExpressionPart>();
            int processedUntil = 0;
            foreach (Match match in matches)
            {
                parts.Add(CreateTextPart(expression.Substring(processedUntil, match.Index - processedUntil)));
                parts.Add(new AnalyzedStepDefinitionExpressionParameterPart(match.Value));
                processedUntil = match.Index + match.Length;
            }
            parts.Add(CreateTextPart(expression.Substring(processedUntil)));

            return new AnalyzedStepDefinitionExpression(parts.ToArray());
        }

        private AnalyzedStepDefinitionExpressionTextPart CreateTextPart(string text)
        {
            var isSimpleText = IsSimpleText(text);
            return new AnalyzedStepDefinitionExpressionTextPart(text, isSimpleText);
        }

        private static bool IsSimpleText(string text)
        {
            //TODO: maybe there is a smarter/more proper way
            text = text.Replace(' ', '_');
            return Regex.Escape(text) == text;
        }
    }
}