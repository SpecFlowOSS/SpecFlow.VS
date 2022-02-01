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
    [InlineData("DS_3.9.40_nunit_1194832604")]
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

        var targetAssemblyFile = FileDetails.FromPath(projectGenerator.TargetFolder, projectGenerator.GetOutputAssemblyPath());

        var logger = new TestConsoleLogger();
        var consoleRunner = new Runner(logger);
        var connectorFile = typeof(DiscoveryCommand).Assembly.GetLocation();

        var psiEx = new ProcessStartInfoEx(
            projectGenerator.TargetFolder,
            connectorFile,
            $"{DiscoveryCommand.CommandName} {targetAssemblyFile}"
        );
#if NETCOREAPP
        psiEx = new ProcessStartInfoEx(
            projectGenerator.TargetFolder,
            "dotnet",
            $"{connectorFile} {DiscoveryCommand.CommandName} {targetAssemblyFile}"
        );
#endif
        _testOutputHelper.WriteLine($"{psiEx.ExecutablePath} {psiEx.Arguments}");
        //act

        var result = new ProcessHelper()
            .RunProcess(psiEx);

        //assert
        _testOutputHelper.ApprovalsVerify(new StringBuilder()
                .Append($"stdout:{result.StdOutput}")
                .AppendLine($"stderr:{result.StdError}")
                .Append($"resultCode:{result.ExitCode}"),
            XunitExtensions.StackTraceScrubber);
    }

    private record GeneratedProjectTestsData(GeneratorOptions GeneratorOptions);
}
