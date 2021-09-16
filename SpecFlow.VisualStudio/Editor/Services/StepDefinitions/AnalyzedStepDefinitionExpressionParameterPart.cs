namespace SpecFlow.VisualStudio.Editor.Services.StepDefinitions
{
    public record AnalyzedStepDefinitionExpressionParameterPart : AnalyzedStepDefinitionExpressionPart
    {
        public string ParameterExpression { get; }

        public AnalyzedStepDefinitionExpressionParameterPart(string parameterExpression)
        {
            ParameterExpression = parameterExpression;
        }

        public override string ExpressionText => ParameterExpression;

        public override string ToString()
        {
            return $"Parameter:`{ExpressionText}`";
        }
    }
}