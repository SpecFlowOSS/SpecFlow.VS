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
    [InlineData("DS_3.9.40_nunit_nprj_net35_bt_992117478")]
    [InlineData("DS_3.9.40_nunit_nprj_net452_bt_992117478")]
    [InlineData("DS_3.9.40_nunit_nprj_net461_bt_992117478")]
    [InlineData("DS_3.9.40_nunit_nprj_net472_bt_992117478")]
    [InlineData("DS_3.9.40_nunit_bt_992117478")]
    [InlineData("DS_3.9.40_nunit_bt_1194832604")]
    [InlineData("DS_3.9.40_nunit_nprj_bt_992117478")]
#if NET
    [InlineData("DS_3.9.40_nunit_nprj_netcoreapp20_bt_992117478")]
    [InlineData("DS_3.9.40_nunit_nprj_netcoreapp21_bt_992117478")]
    [InlineData("DS_3.9.40_nunit_nprj_netcoreapp22_bt_992117478")]
    [InlineData("DS_3.9.40_nunit_nprj_netcoreapp30_bt_992117478")]
    [InlineData("DS_3.9.40_nunit_nprj_netcoreapp31_bt_992117478")]
    [InlineData("DS_3.9.40_nunit_nprj_net5.0_bt_992117478")]
    [InlineData("DS_3.9.40_nunit_nprj_net6.0_bt_992117478")]
    [InlineData("DS_3.9.22_nunit_nprj_net6.0_bt_992117478")]
    [InlineData("DS_3.9.8_nunit_nprj_net6.0_bt_992117478")]
    [InlineData("DS_3.8.14_nunit_nprj_net6.0_bt_992117478")]
    [InlineData("DS_3.7.38_nunit_nprj_net6.0_bt_992117478")]
    [InlineData("DS_3.7.13_nunit_nprj_net6.0_bt_992117478")]
    [InlineData("DS_3.6.23_nunit_nprj_net6.0_bt_992117478")]
    [InlineData("DS_3.5.14_nunit_nprj_net6.0_bt_992117478")]
    [InlineData("DS_3.5.5_nunit_nprj_net6.0_bt_992117478")]
    [InlineData("DS_3.3.30_nunit_nprj_net6.0_bt_992117478")]
    [InlineData("DS_3.1.97_nunit_nprj_net6.0_bt_992117478")]
#endif
    public void Approval(string testName)
    {
        //arrange
        var namer = new PresetApprovalNamer(testName + ".json");
        Approvals.RegisterDefaultNamerCreation(() => namer);

        var testDataFile = FileDetails.FromPath(namer.SourcePath, namer.Name);

        var content = File.ReadAllText(testDataFile);
        var testData = JsonConvert.DeserializeObject<GeneratedProjectTestsData>(content);

        testData.GeneratorOptions.IsBuilt = true;
        var projectGenerator = testData.GeneratorOptions.CreateProjectGenerator(s => _testOutputHelper.WriteLine(s));
        projectGenerator.Generate();

        var targetAssemblyFile =
            FileDetails.FromPath(projectGenerator.TargetFolder, projectGenerator.GetOutputAssemblyPath());
        var outputFolder = Path.GetDirectoryName(targetAssemblyFile)!;

        var connectorFile = typeof(DiscoveryCommand).Assembly.GetLocation();

        Option<FileDetails> configFile = testData.ConfigFile?.Map(s => FileDetails.FromPath(outputFolder, s)) ?? None<FileDetails>.Value;

        //act
        var result = Invoke(targetAssemblyFile, projectGenerator, connectorFile, configFile);

        //assert
        _testOutputHelper.ApprovalsVerify(new StringBuilder()
                .Append($"stdout:{result.StdOutput}")
                .AppendLine($"stderr:{result.StdError}")
                .Append($"resultCode:{result.ExitCode}"),
            rawContent => TargetFolderScrubber(rawContent, projectGenerator.TargetFolder)
                .Map(r => Regex.Replace(r, "(.*\r\n)*>>>>>>>>>>\r\n", ""))
                .Map(r => Regex.Replace(r, "<<<<<<<<<<(.*[\r\n])*.*", ""))
                .Map(XunitExtensions.StackTraceScrubber));
    }

    private ProcessResult Invoke(FileDetails targetAssemblyFile, IProjectGenerator projectGenerator,
        FileDetails connectorFile, Option<FileDetails> configFile)
    {
        var psiEx = new ProcessStartInfoEx(
            projectGenerator.TargetFolder,
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

    private static string TargetFolderScrubber(string content, string targetFolder) =>
        content
            .Replace(targetFolder, "<<targetFolder>>")
            .Replace(targetFolder.Replace("\\", "\\\\"), "<<targetFolder>>");

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

    private record GeneratedProjectTestsData(string? ConfigFile, GeneratorOptions GeneratorOptions);
}
