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
            var parts = SplitExpressionByGroups(expression);
            return new AnalyzedStepDefinitionExpression(parts);
        }

        private ImmutableArray<AnalyzedStepDefinitionExpressionPart> SplitExpressionByGroups(string regexString)
        {
            var parts = new List<AnalyzedStepDefinitionExpressionPart>();
            var unescapedStringBuilder = new StringBuilder();

            var maskChar = '\\';
            var groupOpenChar = '(';
            var maskedRegexChars = new[] { maskChar, '+', '.', '*', '?', '|', '{', '[', groupOpenChar, '^', '$', '#' };
            int position = 0;
            string unescapedString;
            while (position < regexString.Length)
            {
                int index = regexString.IndexOfAny(maskedRegexChars, position);
                if (index < 0)
                {
                    unescapedString = regexString.Substring(position);
                    unescapedStringBuilder.Append(unescapedString);
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

                    unescapedString = unescapedStringBuilder.ToString();
                    parts.Add(CreateTextPart(unescapedString));
                    unescapedStringBuilder = new StringBuilder();
                    var groupCloseIndex = FindGroupCloseIndex(regexString, index) + 1;
                    var parameter = regexString.Substring(index, groupCloseIndex - index);
                    parts.Add(new AnalyzedStepDefinitionExpressionParameterPart(parameter));
                    position = groupCloseIndex;
                }
                else
                {
                    return ImmutableArray.Create(CreateTextPart(regexString));
                }
            }

            unescapedString = unescapedStringBuilder.ToString();
            parts.Add(CreateTextPart(unescapedString));
            return parts.ToImmutableArray();
        }

        private AnalyzedStepDefinitionExpressionPart CreateTextPart(string text)
        {
            return IsSimpleText(text)
                ? new AnalyzedStepDefinitionExpressionSimpleTextPart(text)
                : new AnalyzedStepDefinitionExpressionComplexTextPart(text);
        }

        private static bool IsSimpleText(string text)
        {
            //TODO: maybe there is a smarter/more proper way
            text = text.Replace(' ', '_');
            return Regex.Escape(text) == text;
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