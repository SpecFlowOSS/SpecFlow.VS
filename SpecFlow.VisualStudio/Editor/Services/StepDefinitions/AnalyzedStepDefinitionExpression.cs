using System.Collections.Generic;
using System.Linq;

namespace SpecFlow.VisualStudio.Editor.Services.StepDefinitions
{
    public class AnalyzedStepDefinitionExpression
    {
        public AnalyzedStepDefinitionExpression(AnalyzedStepDefinitionExpressionPart[] parts)
        {
            Parts = parts;
        }

        public AnalyzedStepDefinitionExpressionPart[] Parts { get; }

        public bool ContainsOnlySimpleText => Parts.OfType<AnalyzedStepDefinitionExpressionTextPart>().All(p => p.IsSimpleText);
        public IEnumerable<AnalyzedStepDefinitionExpressionParameterPart> ParameterParts => Parts.OfType<AnalyzedStepDefinitionExpressionParameterPart>();
    }
}