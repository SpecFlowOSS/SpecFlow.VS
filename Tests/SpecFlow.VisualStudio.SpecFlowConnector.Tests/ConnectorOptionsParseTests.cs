namespace SpecFlow.VisualStudio.SpecFlowConnector.Tests;

[UseReporter /*(typeof(VisualStudioReporter))*/]
[UseApprovalSubdirectory("ApprovalTestData\\ConnectorOptionsParse")]
public class ConnectorOptionsParseTests
{
    public static IEnumerable<LabeledTestData<(string[] args, string expected)>> TestCases =
        new LabeledTestData<(string[] args, string expected)>[]
        {
            new("discovery assembly",
                (new[] {"discovery", "../targetAssembly.dll"},
                    @"Right(System.Exception, SpecFlowConnector.ConnectorOptions)DiscoveryOptions { DebugMode = False, AssemblyFile = <<targetPath>>\targetAssembly.dll, ConfigFile = , ConnectorFolder = <<connectorPath>> }")),
            new("discovery assembly with configuration",
                (new[] {"discovery", "../targetAssembly.dll", "../configuration.json"},
                    @"Right(System.Exception, SpecFlowConnector.ConnectorOptions)DiscoveryOptions { DebugMode = False, AssemblyFile = <<targetPath>>\targetAssembly.dll, ConfigFile = <<targetPath>>\configuration.json, ConnectorFolder = <<connectorPath>> }")),
            new("Missing arguments",
                (Array.Empty<string>(),
                    @"Left(System.Exception, SpecFlowConnector.ConnectorOptions)System.ArgumentException: Command is missing!")),
            new("Invalid command",
                (new[] {"xxx"},
                    @"Left(System.Exception, SpecFlowConnector.ConnectorOptions)System.ArgumentException: Invalid command: xxx")),
            new("debug",
                (new[] {"discovery", "--debug", "../targetAssembly.dll"},
                    @"Right(System.Exception, SpecFlowConnector.ConnectorOptions)DiscoveryOptions { DebugMode = True, AssemblyFile = <<targetPath>>\targetAssembly.dll, ConfigFile = , ConnectorFolder = <<connectorPath>> }")),
            new("debug must be after command",
                (new[] {"--debug", "yyy"},
                    "Left(System.Exception, SpecFlowConnector.ConnectorOptions)System.ArgumentException: Invalid command: --debug")),
            new("discovery command without arguments",
                (new[] {"discovery"},
                    "Left(System.Exception, SpecFlowConnector.ConnectorOptions)System.InvalidOperationException: Usage: discovery <test-assembly-path> [<configuration-file-path>]"))
        };

    private readonly ITestOutputHelper _testOutputHelper;

    public ConnectorOptionsParseTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Theory]
    [LabeledMemberData(nameof(TestCases))]
    public void Approval(LabeledTestData<(string[] args, string expected)> @case)
    {
        //arrange
        NamerFactory.AdditionalInformation = @case.Label.Replace(' ', '_');

        //act
        Either<Exception, ConnectorOptions> result = ConnectorOptions.Parse(@case.Data.args);

        //assert
        string asString = $"{result}";
        _testOutputHelper.WriteLine("---------------------------result- ----------------------------------");
        _testOutputHelper.WriteLine(asString);
        asString = asString.Replace(FileDetails.FromPath("this").DirectoryName.Reduce("?"), "<<connectorPath>>");
        asString = asString.Replace(FileDetails.FromPath("../this").DirectoryName.Reduce("?"), "<<targetPath>>");
        _testOutputHelper.WriteLine("--------------------------scrubbed-----------------------------------");
        _testOutputHelper.WriteLine(asString);
        _testOutputHelper.WriteLine("--------------------------expected-----------------------------------");
        _testOutputHelper.WriteLine(@case.Data.expected);
        Approvals.AssertText(@case.Data.expected, asString);
    }
}
