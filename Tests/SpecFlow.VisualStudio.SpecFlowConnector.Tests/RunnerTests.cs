﻿using System.IO.Abstractions.TestingHelpers;
using Castle.DynamicProxy.Generators.Emitters;
using SpecFlowConnector;

namespace SpecFlow.VisualStudio.SpecFlowConnector.Tests;

[UseReporter /*(typeof(VisualStudioReporter))*/]
[UseApprovalSubdirectory("ApprovalTestData\\Runner")]
public class RunnerTests
{
    public static IEnumerable<LabeledTestData<(string[] args, string expected)>> TestCases =
        new LabeledTestData<(string[] args, string expected)>[]
        {
            new("discovery assembly", (new[] {"discovery", "targetAssembly.dll"}, "?")),
            new("discovery assembly with configuration", (new[] {"discovery", "targetAssembly.dll", "configuration.json"}, "?")),
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
        var resultCode = consoleRunner.Run(@case.Data.args, _ => GetType().Assembly, new MockFileSystem());

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
        var resultCode = consoleRunner.Run(new []{command, "testAssembly.dll"}, s => throw new Exception("unexpected failure"), new MockFileSystem());

        //assert
        var output = logger.ToString();
        _testOutputHelper.WriteLine(output);
        output.Should().StartWith("Debug ");
        output.Should().Contain("Error ");
    }
}