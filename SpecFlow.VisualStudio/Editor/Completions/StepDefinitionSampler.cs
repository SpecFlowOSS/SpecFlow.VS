using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SpecFlow.VisualStudio.Discovery;
using SpecFlow.VisualStudio.Editor.Services.StepDefinitions;
using SpecFlow.VisualStudio.SpecFlowConnector.Models;

namespace SpecFlow.VisualStudio.Editor.Completions
{
    public class StepDefinitionSampler
    {
        public string GetStepDefinitionSample(ProjectStepDefinitionBinding stepDefinitionBinding)
        {
            var regexTextCore = stepDefinitionBinding.Expression;

            IStepDefinitionExpressionAnalyzer analyzer = new RegexStepDefinitionExpressionAnalyzer();
            var analyzedStepDefinitionExpression = analyzer.Parse(regexTextCore);
            if (analyzedStepDefinitionExpression.Parts.Length == 1)
            {
                return analyzedStepDefinitionExpression.Parts[0].ExpressionText;
            }

            var completionTextBuilder = new StringBuilder();
            for (int i = 0; i < analyzedStepDefinitionExpression.Parts.Length; i+=2)
            {
                completionTextBuilder.Append(analyzedStepDefinitionExpression.Parts[i].ExpressionText);
                if (i < analyzedStepDefinitionExpression.Parts.Length - 1)
                {
                    completionTextBuilder.Append("[");
                    completionTextBuilder.Append(GetPlaceHolderText(stepDefinitionBinding, i/2));
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
    }
}
