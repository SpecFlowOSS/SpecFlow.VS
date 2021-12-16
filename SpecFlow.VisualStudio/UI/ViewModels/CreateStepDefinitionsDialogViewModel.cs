using System;
using System.Linq;
using SpecFlow.VisualStudio.Snippets;

namespace SpecFlow.VisualStudio.UI.ViewModels;

public class CreateStepDefinitionsDialogViewModel
{
#if DEBUG
    public static CreateStepDefinitionsDialogViewModel DesignData = new()
    {
        ClassName = "MyFeatureSteps",
        ExpressionStyle = SnippetExpressionStyle.CucumberExpression,
        Items = new List<StepDefinitionSnippetItemViewModel>
        {
            new()
            {
                Snippet = @"[Given(@""there is a simple SpecFlow project for (.*)"")]
public void GivenThereIsASimpleSpecFlowProjectForVersion(Version specFlowVersion)
{
    throw new PendingStepException();
}"
            },
            new()
            {
                Snippet = @"[When(@""there is a simple SpecFlow project for (.*)"")]
public void GivenThereIsASimpleSpecFlowProjectForVersion(Version specFlowVersion)
{
    throw new PendingStepException();
}"
            },
            new()
            {
                Snippet = @"[When(@""there is a simple SpecFlow project for (.*)"")]
public void GivenThereIsASimpleSpecFlowProjectForVersion(Version specFlowVersion)
{
    throw new PendingStepException();
}"
            }
        }
    };
#endif
    public string ClassName { get; set; }
    public SnippetExpressionStyle ExpressionStyle { get; set; }
    public List<StepDefinitionSnippetItemViewModel> Items { get; set; } = new();
    public CreateStepDefinitionsDialogResult Result { get; set; }
}
