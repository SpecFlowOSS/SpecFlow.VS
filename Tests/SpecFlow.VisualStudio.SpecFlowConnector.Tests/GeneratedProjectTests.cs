using Newtonsoft.Json;
using SpecFlow.SampleProjectGenerator;

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

        var logger = new TestConsoleLogger();
        var consoleRunner = new Runner(logger);
        var targetAssemblyFile = FileDetails.FromPath(projectGenerator.TargetFolder, projectGenerator.GetOutputAssemblyPath());

        //act
        var resultCode = consoleRunner.Run(new[]{DiscoveryCommand.CommandName, targetAssemblyFile}, Assembly.LoadFrom);

        //assert
        _testOutputHelper.ApprovalsVerify(new StringBuilder()
                .AppendLine($"stdout:{logger[LogLevel.Info]}")
                .AppendLine($"stderr:{logger[LogLevel.Error]}")
                .Append($"resultCode:{resultCode}"),
            XunitExtensions.StackTraceScrubber);
    }

    private record GeneratedProjectTestsData(GeneratorOptions GeneratorOptions);
}
