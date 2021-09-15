using SpecFlow.VisualStudio.Discovery;

namespace SpecFlow.VisualStudio.Editor.Services.StepDefinitions
{
    public interface IStepDefinitionExpressionAnalyzer
    {
        AnalyzedStepDefinitionExpression Parse(string expression);
    }
}