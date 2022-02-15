namespace SpecFlow.VisualStudio.SpecFlowConnector.Tests;

[UseReporter /*(typeof(VisualStudioReporter))*/]
[UseApprovalSubdirectory("ApprovalTestData")]
public class RunnerTests : ApprovalTestBase
{
    public static IEnumerable<LabeledTestData<(string[] args, string expected)>> TestCases =
        new LabeledTestData<(string[] args, string expected)>[]
        {
            new("discovery assembly", (new[] {"discovery", "targetAssembly.dll"}, "?")),
            new("discovery assembly with configuration",
                (new[] {"discovery", "targetAssembly.dll", "configuration.json"}, "?"))
        };

    private readonly ITestOutputHelper _testOutputHelper;

    public RunnerTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Theory]
    [InlineData("SpecFlow.VisualStudio.Specs")]
    public void Approval(string testName)
    {
        //arrange
        var testData = ArrangeTestData<RunnerTestData>(testName);

        //act
        var result = Invoke(testData.TargetFolder, testData.TestAssembly, testData.ConfigFile);

        //assert
        Assert(result, testData.TargetFolder);
    }

    private record RunnerTestData(string TargetFolder, string TestAssembly, string? ConfigFile);
}
