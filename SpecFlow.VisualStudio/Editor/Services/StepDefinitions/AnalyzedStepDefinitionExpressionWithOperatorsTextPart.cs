namespace SpecFlow.VisualStudio.Editor.Services.StepDefinitions
{
    public record AnalyzedStepDefinitionExpressionWithOperatorsTextPart : AnalyzedStepDefinitionExpressionPart
    {
        public string Text { get; }
        public override string ExpressionText => Text;

        public AnalyzedStepDefinitionExpressionWithOperatorsTextPart(string text)
        {
            Text = text;
        }

        public override string ToString()
        {
            return $"escaped text:`{ExpressionText}`";
        }
    }
}
