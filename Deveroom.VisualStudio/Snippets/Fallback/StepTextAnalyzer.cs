using System.Globalization;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Deveroom.VisualStudio.Snippets.Fallback
{
    // imported from SpecFlow v2.4

    public class AnalyzedStepText
    {
        public readonly List<string> TextParts = new List<string>();
        public readonly List<AnalyzedStepParameter> Parameters = new List<AnalyzedStepParameter>();
    }

    public class AnalyzedStepParameter
    {
        public readonly string Type;
        public readonly string Name;
        public readonly string RegexPattern;

        public AnalyzedStepParameter(string type, string name, string regexPattern = null)
        {
            this.Type = type;
            this.Name = name;
            this.RegexPattern = regexPattern;
        }
    }

    public interface IStepTextAnalyzer
    {
        AnalyzedStepText Analyze(string stepText, CultureInfo bindingCulture);
    }

    public class StepTextAnalyzer : IStepTextAnalyzer
    {
        private readonly List<string> _usedParameterNames = new List<string>();
        public AnalyzedStepText Analyze(string stepText, CultureInfo bindingCulture)
        {
            var result = new AnalyzedStepText();

            var paramMatches = RecognizeQuotedTexts(stepText).Concat(RecognizeIntegers(stepText)).Concat(RecognizeDecimals(stepText, bindingCulture))
                .OrderBy(m => m.Index).ThenByDescending(m => m.Length);

            int textIndex = 0;
            foreach (var paramMatch in paramMatches)
            {
                if (paramMatch.Index < textIndex)
                    continue;

                const string singleQuoteRegexPattern = "[^']*";
                const string doubleQuoteRegexPattern = "[^\"\"]*";
                const string defaultRegexPattern = ".*";

                string regexPattern = defaultRegexPattern;
                string value = paramMatch.Value;
                int index = paramMatch.Index;

                if (index > 0)
                    switch (stepText[index - 1])
                    {
                        case '\"':
                            regexPattern = doubleQuoteRegexPattern;
                            break;
                        case '\'':
                            regexPattern = singleQuoteRegexPattern;
                            break;
                    }

                result.TextParts.Add(stepText.Substring(textIndex, index - textIndex));
                result.Parameters.Add(AnalyzeParameter(value, bindingCulture, result.Parameters.Count, regexPattern));
                textIndex = index + value.Length;
            }

            result.TextParts.Add(stepText.Substring(textIndex));
            return result;
        }

        private AnalyzedStepParameter AnalyzeParameter(string value, CultureInfo bindingCulture, int paramIndex, string regexPattern)
        {
            string paramName = StepParameterNameGenerator.GenerateParameterName(value, paramIndex, _usedParameterNames);

            if (int.TryParse(value, NumberStyles.Integer, bindingCulture, out _))
                return new AnalyzedStepParameter("Int32", paramName, regexPattern);

            if (decimal.TryParse(value, NumberStyles.Number, bindingCulture, out _))
                return new AnalyzedStepParameter("Decimal", paramName, regexPattern);

            return new AnalyzedStepParameter("String", paramName, regexPattern);
        }

        private static readonly Regex quotesRe = new Regex(@"""(?<param>.*?)""|'(?<param>.*?)'|(?<param>\<.*?\>)");
        private IEnumerable<Capture> RecognizeQuotedTexts(string stepText)
        {
            return quotesRe.Matches(stepText).Cast<Match>().Select(m => (Capture)m.Groups["param"]);
        }

        private static readonly Regex intRe = new Regex(@"-?\d+");
        private IEnumerable<Capture> RecognizeIntegers(string stepText)
        {
            return intRe.Matches(stepText).Cast<Capture>();
        }

        private IEnumerable<Capture> RecognizeDecimals(string stepText, CultureInfo bindingCulture)
        {
            Regex decimalRe = new Regex(string.Format(@"-?\d+{0}\d+", bindingCulture.NumberFormat.NumberDecimalSeparator));
            return decimalRe.Matches(stepText).Cast<Capture>();
        }
    }
}