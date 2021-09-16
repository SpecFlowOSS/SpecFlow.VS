#nullable enable
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Text.RegularExpressions;

namespace SpecFlow.VisualStudio.Editor.Services.StepDefinitions
{
    public class RegexStepDefinitionExpressionAnalyzer : IStepDefinitionExpressionAnalyzer
    {
        public AnalyzedStepDefinitionExpression Parse(string expression)
        {
            if (!SplitRegexByGroups(expression, out var regexParts))
                return new AnalyzedStepDefinitionExpression(
                    ImmutableArray.Create<AnalyzedStepDefinitionExpressionPart>(
                        CreateTextPart(expression)
                        )
                    );

            var parts = new List<AnalyzedStepDefinitionExpressionPart>();
            int i = 0;
            for (; i < regexParts.Length-1; i++)
            {
                var regexPart = regexParts[i];
                parts.Add(CreateTextPart(regexPart));
                parts.Add(new AnalyzedStepDefinitionExpressionParameterPart("??"));
            }
            parts.Add(CreateTextPart(regexParts[i]));
            return new AnalyzedStepDefinitionExpression(parts.ToImmutableArray());
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

        private bool SplitRegexByGroups(string regexString, out string[] unescapedStrings)
        {
            unescapedStrings = null;
            List<string> unescapedStringsList = null;
            var unescapedStringBuilder = new StringBuilder();

            var maskChar = '\\';
            var groupOpenChar = '(';
            var maskedRegexChars = new[] { maskChar, '+', '.', '*', '?', '|', '{', '[', groupOpenChar, '^', '$', '#' };
            int position = 0;
            while (position < regexString.Length)
            {
                int index = regexString.IndexOfAny(maskedRegexChars, position);
                if (index < 0)
                {
                    unescapedStringBuilder.Append(regexString.Substring(position));
                    break;
                }

                if (regexString[index] == maskChar && index < regexString.Length - 1)
                {
                    if (index > position)
                        unescapedStringBuilder.Append(regexString.Substring(position, index - position));

                    unescapedStringBuilder.Append(regexString[index + 1]);
                    position = index + 2;
                }
                else if (regexString[index] == groupOpenChar && !IsNonCapturingGroup(regexString, index))
                {
                    if (index > position)
                        unescapedStringBuilder.Append(regexString.Substring(position, index - position));

                    unescapedStringsList = unescapedStringsList ?? new List<string>();
                    unescapedStringsList.Add(unescapedStringBuilder.ToString());
                    unescapedStringBuilder = new StringBuilder();
                    position = FindGroupCloseIndex(regexString, index) + 1;
                }
                else
                {
                    return false;
                }
            }

            unescapedStringsList = unescapedStringsList ?? new List<string>();
            unescapedStringsList.Add(unescapedStringBuilder.ToString());
            unescapedStrings = unescapedStringsList.ToArray();
            return true;
        }

        private int FindGroupCloseIndex(string regexString, int openPosition)
        {
            int nesting = 0;
            for (int i = openPosition; i < regexString.Length; i++)
            {
                if (regexString[i] == '\\')
                    i++;
                else if (regexString[i] == '(')
                    nesting++;
                else if (regexString[i] == ')')
                {
                    nesting--;
                    if (nesting == 0)
                        return i;
                }
            }

            return regexString.Length;
        }

        private bool IsNonCapturingGroup(string regexString, int index)
        {
            return index + 2 < regexString.Length &&
                regexString[index + 1] == '?' && regexString[index + 2] == ':';
        }

    }
}