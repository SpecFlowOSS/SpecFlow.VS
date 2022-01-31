namespace SpecFlow.VisualStudio.SpecFlowConnector.Tests;

[UseReporter /*(typeof(VisualStudioReporter))*/]
[UseApprovalSubdirectory("ApprovalTestData")]
public class ConsoleRunnerTests
{
    public static IEnumerable<object[]> TestCases = new List<object[]>
    {
        new object[] {"Missing arguments", Array.Empty<string>()},
        new object[] {"Invalid command", new[] {"xxx"}},
        new object[] {"discovery command", new[] {"discovery"}},
        new object[] {"debug", new[] {"xxx", "--debug"}},
        new object[] {"debug must be after command", new[] {"--debug", "yyy"}}
    };

    private readonly ITestOutputHelper _testOutputHelper;

    public ConsoleRunnerTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Theory]
    [MemberData(nameof(TestCases))]
    public void Approval(string testName, string[] args)
    {
        //arrange
        NamerFactory.AdditionalInformation = testName.Replace(' ', '_');
        var logger = new StringBuilderLogger();
        var consoleRunner = new Runner(logger);

        //act
        var resultCode = consoleRunner.Run(args);

        //assert
        _testOutputHelper.ApprovalsVerify(new StringBuilder()
                .AppendLine($"stdout:{logger[LogLevel.Info]}")
                .AppendLine($"stderr:{logger[LogLevel.Error]}")
                .Append($"resultCode:{resultCode}"),
            XunitExtensions.StackTraceScrubber);
    }
}
