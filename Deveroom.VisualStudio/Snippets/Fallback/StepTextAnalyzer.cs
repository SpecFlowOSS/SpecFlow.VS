using System.Globalization;
using System.Linq;
using System.Collections.Generic;

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
}