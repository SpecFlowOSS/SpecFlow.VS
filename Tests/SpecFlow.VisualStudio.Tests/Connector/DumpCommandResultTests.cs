namespace SpecFlow.VisualStudio.Tests.Connector;

[UseReporter /*(typeof(VisualStudioReporter))*/]
[UseApprovalSubdirectory("../ApprovalTestData")]
public class DumpCommandResultTests
{
    public static IEnumerable<LabeledTestData<Exception>> TestData = new List<LabeledTestData<Exception>>
    {
        new("SimpleException", new Exception().WithStackTrace()),
        new("SimpleExceptionWithMessage", new Exception("Message").WithStackTrace()),
        new("NotImplementedException", new NotImplementedException().WithStackTrace()),
        new("NotImplementedExceptionWithMessage", new NotImplementedException("Message").WithStackTrace()),
        new(
            "WithOneException",
            new Exception("Message1", new NotImplementedException("Message2").WithStackTrace())
        ),
        new(
            "NestedExceptions",
            new Exception("Message1",
                    new NotImplementedException("Message2"
                            , new InvalidOperationException("Messasge3")
                                .WithStackTrace())
                        .WithStackTrace())
                .WithStackTrace()
        ),
        new(
            "AggregateException",
            new AggregateException("Message1",
                new NotImplementedException("Message2").WithStackTrace(),
                new InvalidOperationException("Messasge3").WithStackTrace()
            ).WithStackTrace()
        ),
        new(
            "NestedAggregateException",
            new AggregateException("Message1",
                new NotImplementedException("Message2"
                    , new AggregateException("Message3"
                        , new Exception("Message4").WithStackTrace()
                    ).WithStackTrace()
                ).WithStackTrace(),
                new AggregateException("Message5"
                    , new InvalidOperationException("Message6").WithStackTrace()
                ).WithStackTrace()
            ).WithStackTrace()
        )
    };

    private readonly ITestOutputHelper _testOutputHelper;

    public DumpCommandResultTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Theory]
    [LabeledMemberData(nameof(TestData))]
    public void Approval(LabeledTestData<Exception> testTestData)
    {
        NamerFactory.AdditionalInformation = testTestData.Label;

        var dump = testTestData.Data.Dump();

        _testOutputHelper.ApprovalsVerify(dump, XunitExtensions.StackTraceScrubber);
    }
}
