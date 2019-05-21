using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Deveroom.VisualStudio.Discovery;
using Deveroom.VisualStudio.SpecFlowConnector.Models;

namespace Deveroom.VisualStudio.Editor.Completions
{
    public class StepDefinitionSampler
    {
        public string GetStepDefinitionSample(ProjectStepDefinitionBinding stepDefinitionBinding)
        {
            var regexTextCore = stepDefinitionBinding.Regex.ToString().TrimStart('^').TrimEnd('$');

            if (!SplitRegexByGroups(regexTextCore, out var regexParts))
                return regexTextCore;
            var completionTextBuilder = new StringBuilder();
            for (int i = 0; i < regexParts.Length; i++)
            {
                completionTextBuilder.Append(regexParts[i]);
                if (i < regexParts.Length - 1)
                {
                    completionTextBuilder.Append("[");
                    completionTextBuilder.Append(GetPlaceHolderText(stepDefinitionBinding, i));
                    completionTextBuilder.Append("]");
                }
            }

            return completionTextBuilder.ToString();
        }

        private string GetPlaceHolderText(ProjectStepDefinitionBinding stepDefinitionBinding, int groupIndex)
        {
            if (stepDefinitionBinding.Implementation?.ParameterTypes == null ||
                groupIndex >= stepDefinitionBinding.Implementation.ParameterTypes.Length)
                return "???";

            var typeName = stepDefinitionBinding.Implementation.ParameterTypes[groupIndex];
            switch (typeName)
            {
                case TypeShortcuts.Int32Type:
                    return "int";
                case TypeShortcuts.StringType:
                    return "string";
            }
            return typeName.Split('.').Last();
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
