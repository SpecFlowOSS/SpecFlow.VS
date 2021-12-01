namespace SpecFlow.VisualStudio.Discovery;

public record CSharpStepDefinitionFile(string StepDefinitionPath, string Content)
    : StepDefinitionFile(StepDefinitionPath, Content)
{
}

public record StepDefinitionFile(string StepDefinitionPath, string Content);
