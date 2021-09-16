namespace SpecFlow.VisualStudio.Editor.Services.StepDefinitions
{
    public record AnalyzedStepDefinitionExpressionComplexTextPart : AnalyzedStepDefinitionExpressionPart
    {
        public string Text { get; }
        public override string ExpressionText => Text;

        public AnalyzedStepDefinitionExpressionComplexTextPart(string text)
        {
            Text = text;
        }

        public override string ToString()
        {
            return $"escaped text:`{ExpressionText}`";
        }
    }
}