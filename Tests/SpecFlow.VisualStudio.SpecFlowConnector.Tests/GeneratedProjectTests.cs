using System.Diagnostics.Eventing.Reader;
using System.IO.Abstractions.TestingHelpers;

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
        testName += ".json";
        NamerFactory.AdditionalInformation = testName;
        var namer = Approvals.GetDefaultNamer();
        var testDataFile = FileDetails.FromPath(namer.SourcePath, namer.Name);

        NamerFactory.AdditionalInformation = testName;

        var content = File.ReadAllText(testDataFile);
        var testData = JsonConvert.DeserializeObject<GeneratedProjectTestsData>(content);

        testData.GeneratorOptions.CreatedFor = "GPT";
        testData.GeneratorOptions.IsBuilt = true;
        var projectGenerator = testData.GeneratorOptions.CreateProjectGenerator(s => _testOutputHelper.WriteLine(s));
        projectGenerator.Generate();

        var targetAssemblyFile =
            FileDetails.FromPath(projectGenerator.TargetFolder, projectGenerator.GetOutputAssemblyPath());

        var connectorFile = typeof(DiscoveryCommand).Assembly.GetLocation();
        
        //act
        ProcessResult result = Debugger.IsAttached 
            ? InvokeInMemory(targetAssemblyFile)
            : InvokeAsProcess(projectGenerator, connectorFile, targetAssemblyFile);

        //assert
        _testOutputHelper.ApprovalsVerify(new StringBuilder()
                .Append($"stdout:{result.StdOutput}")
                .AppendLine($"stderr:{result.StdError}")
                .Append($"resultCode:{result.ExitCode}"),
            rawContent => TargetFolderScrubber(rawContent, projectGenerator.TargetFolder)
                .Map(XunitExtensions.StackTraceScrubber));
    }

    private static string TargetFolderScrubber(string content, string targetFolder) 
        => content.Replace(targetFolder, "<<targetFolder>>");

    private static ProcessResult InvokeInMemory(FileDetails targetAssemblyFile)
    {
        var logger = new TestConsoleLogger();
        var consoleRunner = new Runner(logger);
        var mockFileSystem = new MockFileSystem();
        var resultCode = consoleRunner.Run(new[] {DiscoveryCommand.CommandName, targetAssemblyFile},
            Assembly.LoadFrom,
            mockFileSystem);
        var result = new ProcessResult(resultCode, logger[LogLevel.Info], logger[LogLevel.Error], TimeSpan.Zero);
        return result;
    }

    private ProcessResult InvokeAsProcess(IProjectGenerator projectGenerator, FileDetails connectorFile,
        FileDetails targetAssemblyFile)
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
        var result = new ProcessHelper()
            .RunProcess(psiEx);
        return result;
    }

    private record GeneratedProjectTestsData(GeneratorOptions GeneratorOptions);
}
