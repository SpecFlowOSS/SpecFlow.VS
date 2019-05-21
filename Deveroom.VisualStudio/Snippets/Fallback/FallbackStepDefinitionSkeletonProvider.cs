using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Deveroom.VisualStudio.Discovery;

namespace Deveroom.VisualStudio.Snippets.Fallback
{
    public static class FallbackStepDefinitionSkeletonProvider
    {
        public static string GetStepDefinitionSkeletonSnippetFallback(UndefinedStepDescriptor undefinedStep,
            string indent, string newLine, string bindingCultureName)
        {
            var bindingCulture = CultureInfo.GetCultureInfo(bindingCultureName);

            var analyzedStepText = Analyze(undefinedStep, bindingCulture); 

            var regex = GetRegex(analyzedStepText);
            var methodName = GetMethodName(undefinedStep, analyzedStepText);
            var parameters = string.Join(", ", analyzedStepText.Parameters.Select(p => ToDeclaration(p)).ToArray());

            var method = $"[{undefinedStep.ScenarioBlock}(@\"{regex}\")]" + newLine +
                         $"public void {methodName}({parameters})" + newLine +
                         $"{{" + newLine +
                         $"{indent}throw new PendingStepException();" + newLine +
                         $"}}" + newLine;

            return method;
        }

        // imported from SpecFlow v2.4

        private static string GetRegex(AnalyzedStepText stepText)
        {
            StringBuilder result = new StringBuilder();

            result.Append(EscapeRegex(stepText.TextParts[0]));
            for (int i = 1; i < stepText.TextParts.Count; i++)
            {
                result.AppendFormat("({0})", stepText.Parameters[i - 1].RegexPattern);
                result.Append(EscapeRegex(stepText.TextParts[i]));
            }

            return result.ToString();
        }

        private static string EscapeRegex(string text)
        {
            return Regex.Escape(text).Replace("\"", "\"\"").Replace("\\ ", " ");
        }

        private static string GetMethodName(UndefinedStepDescriptor stepInstance, AnalyzedStepText analyzedStepText)
        {
            var keyword = stepInstance.ScenarioBlock.ToString(); //TODO: get lang specific keyword
            return keyword.ToIdentifier() + string.Concat(analyzedStepText.TextParts.ToArray()).ToIdentifier();
        }

        private static AnalyzedStepText Analyze(UndefinedStepDescriptor stepInstance, CultureInfo bindingCulture)
        {
            var stepTextAnalyzer = new StepTextAnalyzer();
            var result = stepTextAnalyzer.Analyze(stepInstance.StepText, bindingCulture);
            if (stepInstance.HasDocString)
                result.Parameters.Add(new AnalyzedStepParameter("String", "multilineText"));
            if (stepInstance.HasDataTable)
                result.Parameters.Add(new AnalyzedStepParameter("Table", "table"));
            return result;
        }

        private static string ToDeclaration(AnalyzedStepParameter parameter)
        {
            return String.Format("{1} {0}", Keywords.EscapeCSharpKeyword(parameter.Name), GetCSharpTypeName(parameter.Type));
        }

        private static string GetCSharpTypeName(string type)
        {
            switch (type)
            {
                case "String":
                    return "string";
                case "Int32":
                    return "int";
                default:
                    return type;
            }
        }
    }
}
