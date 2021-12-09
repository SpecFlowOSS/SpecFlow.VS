namespace SpecFlow.VisualStudio.Discovery;

public record CSharpStepDefinitionFile(DirectoryInfo StepDefinitionPath, string Content)
    : StepDefinitionFile(StepDefinitionPath, Content);

public record StepDefinitionFile(DirectoryInfo StepDefinitionPath, string Content);
