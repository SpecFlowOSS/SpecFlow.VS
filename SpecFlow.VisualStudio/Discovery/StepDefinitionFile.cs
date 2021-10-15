namespace SpecFlow.VisualStudio.Discovery;

public class StepDefinitionFile
{
    public string StepDefinitionPath { get; }
    public string Content { get; }

    public StepDefinitionFile(string stepDefinitionPath, string content)
    {
        StepDefinitionPath = stepDefinitionPath;
        Content = content;
    }
}
