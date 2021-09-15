namespace SpecFlow.VisualStudio.Editor.Services.StepDefinitions
{
    public class AnalyzedStepDefinitionExpressionParameterPart : AnalyzedStepDefinitionExpressionPart
    {
        public string ParameterExpression { get; }

        public AnalyzedStepDefinitionExpressionParameterPart(string parameterExpression)
        {
            ParameterExpression = parameterExpression;
        }

        public override string ExpressionText => ParameterExpression;
    }
}