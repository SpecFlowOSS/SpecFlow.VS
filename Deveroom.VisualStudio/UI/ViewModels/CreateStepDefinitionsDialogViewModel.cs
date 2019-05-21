using System;
using System.Collections.Generic;
using System.Linq;

namespace Deveroom.VisualStudio.UI.ViewModels
{
    public class CreateStepDefinitionsDialogViewModel
    {
        public string ClassName { get; set; }
        public List<StepDefinitionSnippetItemViewModel> Items { get; set; } = new List<StepDefinitionSnippetItemViewModel>();
        public CreateStepDefinitionsDialogResult Result { get; set; }

#if DEBUG
        public static CreateStepDefinitionsDialogViewModel DesignData = new CreateStepDefinitionsDialogViewModel()
        {
            ClassName = "MyFeatureSteps",
            Items = new List<StepDefinitionSnippetItemViewModel>()
            {
                new StepDefinitionSnippetItemViewModel
                {
                    Snippet = @"[Given(@""there is a simple SpecFlow project for (.*)"")]
public void GivenThereIsASimpleSpecFlowProjectForVersion(Version specFlowVersion)
{
    throw new PendingStepException();
}"
                },
                new StepDefinitionSnippetItemViewModel
                {
                    Snippet = @"[When(@""there is a simple SpecFlow project for (.*)"")]
public void GivenThereIsASimpleSpecFlowProjectForVersion(Version specFlowVersion)
{
    throw new PendingStepException();
}"
                },
                new StepDefinitionSnippetItemViewModel
                {
                    Snippet = @"[When(@""there is a simple SpecFlow project for (.*)"")]
public void GivenThereIsASimpleSpecFlowProjectForVersion(Version specFlowVersion)
{
    throw new PendingStepException();
}"
                },
            }
        };
#endif
    }
}
