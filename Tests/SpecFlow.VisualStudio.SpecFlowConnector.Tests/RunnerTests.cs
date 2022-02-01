using SpecFlow.VisualStudio.SpecFlowConnector.Tests.AssemblyLoading;

namespace SpecFlow.VisualStudio.SpecFlowConnector.Tests;

[UseReporter /*(typeof(VisualStudioReporter))*/]
[UseApprovalSubdirectory("ApprovalTestData\\Runner")]
public class RunnerTests
{
    public static IEnumerable<LabeledTestData<(string[] args, string expected)>> TestCases =
        new LabeledTestData<(string[] args, string expected)>[]
        {
            new("discovery assembly", (new[] {"discovery", "targetAssembly.dll"}, "?")),
            new("discovery assembly with config", (new[] {"discovery", "targetAssembly.dll", "config.json"}, "?")),
        };

    private readonly ITestOutputHelper _testOutputHelper;

    public RunnerTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Theory]
    [LabeledMemberData(nameof(TestCases))]
    public void Approval(LabeledTestData<(string[] args, string expected)> @case)
    {
        //arrange
        NamerFactory.AdditionalInformation = @case.Label.Replace(' ', '_');
        var logger = new TestConsoleLogger();
        var consoleRunner = new Runner(logger);

        //act
        var resultCode = consoleRunner.Run(@case.Data.args, new StubAssembly().Load);

        //assert
        _testOutputHelper.ApprovalsVerify(new StringBuilder()
                .AppendLine($"stdout:{logger[LogLevel.Info]}")
                .AppendLine($"stderr:{logger[LogLevel.Error]}")
                .Append($"resultCode:{resultCode}"),
            XunitExtensions.StackTraceScrubber);
    }

    [Theory]
    [InlineData("discovery")]
    public void Debug_log_printed_when_fails(string command)
    {
        //arrange
        var logger = new TestConsoleLogger();
        var consoleRunner = new Runner(logger);

        //act
        var resultCode = consoleRunner.Run(new []{command, "testAssembly.dll"}, s => throw new Exception("unexpected failure"));

        //assert
        var output = logger.ToString();
        _testOutputHelper.WriteLine(output);
        output.Should().StartWith("Debug ");
        output.Should().Contain("Error ");
    }
}