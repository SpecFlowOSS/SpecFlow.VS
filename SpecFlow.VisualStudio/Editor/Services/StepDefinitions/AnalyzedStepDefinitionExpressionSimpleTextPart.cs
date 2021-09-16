namespace SpecFlow.VisualStudio.Editor.Services.StepDefinitions
{
    public class AnalyzedStepDefinitionExpressionSimpleTextPart : AnalyzedStepDefinitionExpressionPart
    {
        public string Text { get; }
        public override string ExpressionText => Text;

        public AnalyzedStepDefinitionExpressionSimpleTextPart(string text)
        {
            Text = text;
        }

        public override string ToString()
        {
            return $"simple text:`{ExpressionText}`";
        }
    }
}