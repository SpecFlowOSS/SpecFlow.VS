﻿namespace SpecFlow.VisualStudio.SpecFlowConnector.Tests;

public class ApprovalTestBase
{
    private readonly ITestOutputHelper _testOutputHelper;

    public ApprovalTestBase(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    protected ProcessResult Invoke(string targetFolder, string testAssemblyFileName, string? configFileName)
    {
        var targetAssemblyFile =
            FileDetails.FromPath(targetFolder, testAssemblyFileName);

        var connectorFile = typeof(DiscoveryCommand).Assembly.GetLocation();
        var outputFolder = Path.GetDirectoryName(targetAssemblyFile)!;

        Option<FileDetails> configFile = configFileName?.Map(s => FileDetails.FromPath(outputFolder, s)) ??
                                         None<FileDetails>.Value;

        var psiEx = new ProcessStartInfoEx(
            targetFolder,
            connectorFile,
            string.Empty
        );
#if NETCOREAPP
        psiEx = psiEx with
        {
            ExecutablePath = "dotnet",
            Arguments = $"{connectorFile} "
        };
#endif
        psiEx = psiEx with
        {
            Arguments = psiEx.Arguments +
                        $"{DiscoveryCommand.CommandName} {targetAssemblyFile} {configFile.Map(cf => cf.FullName).Reduce(string.Empty)}"
        };

        _testOutputHelper.WriteLine($"{psiEx.ExecutablePath} {psiEx.Arguments}");

        ProcessResult result = Debugger.IsAttached
            ? InvokeInMemory(psiEx)
            : InvokeAsProcess(psiEx);
        return result;
    }

    private static ProcessResult InvokeInMemory(ProcessStartInfoEx psiEx)
    {
#if NETCOREAPP
        var split = psiEx.Arguments.Split(' ', 2);
        psiEx = psiEx with
        {
            ExecutablePath = split[0],
            Arguments = split[1]
        };
#endif
        Assembly? testAssembly = null;

        var logger = new TestConsoleLogger();
        var consoleRunner = new Runner(logger);
        var mockFileSystem = new FileSystem();
        var resultCode = consoleRunner.Run(
            psiEx.Arguments.Split(' '),
            (ctx, path) => testAssembly ??= ctx.LoadFromAssemblyPath(path),
            mockFileSystem);
        var result = new ProcessResult(resultCode, logger[LogLevel.Info], logger[LogLevel.Error], TimeSpan.Zero);
        return result;
    }

    private ProcessResult InvokeAsProcess(ProcessStartInfoEx psiEx)
    {
        var result = new ProcessHelper()
            .RunProcess(psiEx);
        return result;
    }

    protected void Assert(ProcessResult result, string targetFolder)
    {
        Assert(result, targetFolder, s => s);
    }

    protected void Assert(ProcessResult result, string targetFolder, Func<string, string> scrubber)
    {
        _testOutputHelper.ApprovalsVerify(new StringBuilder().Append((string?) $"stdout:{result.StdOutput}")
                .AppendLine((string?) $"stderr:{result.StdError}")
                .Append($"resultCode:{result.ExitCode}"),
            rawContent => rawContent
                .Map(r => TargetFolderScrubber(r, targetFolder))
                .Map(r => r.Replace(typeof(DiscoveryCommand).Assembly.ToString(), "<connector>"))
                .Map(r => Regex.Replace(r, "errorMessage\": \".+\"", "errorMessage\": \"<errorMessage>\""))
                .Map(r => Regex.Replace(r, "(.*\r\n)*>>>>>>>>>>\r\n", ""))
                .Map(r => Regex.Replace(r, "<<<<<<<<<<(.*[\r\n])*.*", ""))
                .Map(XunitExtensions.StackTraceScrubber)
                .Map(ScrubVolatileParts)
                .Map(scrubber)
        );
    }

    private static string TargetFolderScrubber(string content, string targetFolder) =>
        content
            .Replace(targetFolder, "<<targetFolder>>")
            .Replace(targetFolder.Replace("\\", "\\\\"), "<<targetFolder>>");

    protected static T ArrangeTestData<T>(string testName)
    {
        var namer = new ShortenedUnitTestFrameworkNamer();
        NamerFactory.AdditionalInformation = testName;
        Approvals.RegisterDefaultNamerCreation(() => namer);

        var testDataFile = FileDetails.FromPath(namer.SourcePath, testName + ".json");

        var content = File.ReadAllText(testDataFile);
        var testData = JsonSerializer.Deserialize<T>(content);
        return testData;
    }

    private static string ScrubVolatileParts(string content)
    {
        return content
            .Map(r => JsonSerialization.DeserializeObject<DiscoveryResult>(r)
                .Map(dr => dr with {StepDefinitions = ImmutableArray<StepDefinition>.Empty})
                .Map(dr => dr with {SourceFiles = ImmutableSortedDictionary<string, string>.Empty})
                .Map(dr => dr with {TypeNames = ImmutableSortedDictionary<string, string>.Empty})
                .Map(JsonSerialization.SerializeObject)
                .Reduce($"Cannot deserialize:{r}"));
    }
}
