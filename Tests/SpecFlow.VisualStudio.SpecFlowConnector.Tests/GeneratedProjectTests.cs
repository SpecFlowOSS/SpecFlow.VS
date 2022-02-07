using SpecFlowConnector.Discovery;

namespace SpecFlow.VisualStudio.SpecFlowConnector.Tests;

[UseReporter /*(typeof(VisualStudioReporter))*/]
[UseApprovalSubdirectory("ApprovalTestData\\GeneratedProject")]
public class GeneratedProjectTests
{
    private readonly ITestOutputHelper _testOutputHelper;

    public GeneratedProjectTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Theory]
    [InlineData("DS_2.3.2_nunit_1194832604")]
    [InlineData("DS_GPT_3.9.40_nunit_nprj_net6.0_bt_1194832604")]
    [InlineData("DS_GPT_3.9.40_nunit_bt_1194832604")]
    [InlineData("DS_GPT_3.9.40_nunit_nprj_1194832604")]
    public void Approval(string testName)
    {
        //arrange
        var namer = new PresetApprovalNamer(testName + ".json");
        Approvals.RegisterDefaultNamerCreation(() => namer);

        var testDataFile = FileDetails.FromPath(namer.SourcePath, namer.Name);

        var content = File.ReadAllText(testDataFile);
        var testData = JsonConvert.DeserializeObject<GeneratedProjectTestsData>(content);

        testData.GeneratorOptions.CreatedFor ??= "GPT";
        testData.GeneratorOptions.IsBuilt = true;
        var projectGenerator = testData.GeneratorOptions.CreateProjectGenerator(s => _testOutputHelper.WriteLine(s));
        projectGenerator.Generate();

        var targetAssemblyFile =
            FileDetails.FromPath(projectGenerator.TargetFolder, projectGenerator.GetOutputAssemblyPath());

        var connectorFile = typeof(DiscoveryCommand).Assembly.GetLocation();

        //act
        var result = Invoke(targetAssemblyFile, projectGenerator, connectorFile);

        //assert
        _testOutputHelper.ApprovalsVerify(new StringBuilder()
                .Append($"stdout:{result.StdOutput}")
                .AppendLine($"stderr:{result.StdError}")
                .Append($"resultCode:{result.ExitCode}"),
            rawContent => TargetFolderScrubber(rawContent, projectGenerator.TargetFolder)
                .Map(XunitExtensions.StackTraceScrubber));
    }

    private ProcessResult Invoke(FileDetails targetAssemblyFile, IProjectGenerator projectGenerator,
        FileDetails connectorFile)
    {
        var psiEx = new ProcessStartInfoEx(
            projectGenerator.TargetFolder,
            connectorFile,
            $"{DiscoveryCommand.CommandName} {targetAssemblyFile}"
        );
#if NETCOREAPP
        psiEx = psiEx with
        {
            ExecutablePath = "dotnet",
            Arguments = $"{connectorFile} {DiscoveryCommand.CommandName} {targetAssemblyFile}"
        };
#endif
        _testOutputHelper.WriteLine($"{psiEx.ExecutablePath} {psiEx.Arguments}");

        ProcessResult result = Debugger.IsAttached
            ? InvokeInMemory(psiEx)
            : InvokeAsProcess(psiEx);
        return result;
    }

    private static string TargetFolderScrubber(string content, string targetFolder)
        => content.Replace(targetFolder, "<<targetFolder>>");

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

    private record GeneratedProjectTestsData(GeneratorOptions GeneratorOptions);
}
