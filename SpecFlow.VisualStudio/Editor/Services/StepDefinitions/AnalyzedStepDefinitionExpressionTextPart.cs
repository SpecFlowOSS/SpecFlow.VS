namespace SpecFlow.VisualStudio.Editor.Services.StepDefinitions
{
    public class AnalyzedStepDefinitionExpressionTextPart : AnalyzedStepDefinitionExpressionPart
    {
        public string Text { get; }
        public bool IsSimpleText { get; }
        public override string ExpressionText => Text;

        public AnalyzedStepDefinitionExpressionTextPart(string text, bool isSimpleText)
        {
            Text = text;
            IsSimpleText = isSimpleText;
        }
    }
}