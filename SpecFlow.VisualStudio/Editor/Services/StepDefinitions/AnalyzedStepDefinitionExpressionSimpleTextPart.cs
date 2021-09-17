namespace SpecFlow.VisualStudio.Editor.Services.StepDefinitions
{
    public record AnalyzedStepDefinitionExpressionSimpleTextPart : AnalyzedStepDefinitionExpressionPart
    {
        public string Text { get; }
        public string UnescapedText { get; }
        public override string ExpressionText => Text;

        public AnalyzedStepDefinitionExpressionSimpleTextPart(string text, string unescapedText)
        {
            Text = text;
            UnescapedText = unescapedText ?? text;
        }

        public override string ToString()
        {
            return $"simple text:`{ExpressionText}`";
        }
    }
}