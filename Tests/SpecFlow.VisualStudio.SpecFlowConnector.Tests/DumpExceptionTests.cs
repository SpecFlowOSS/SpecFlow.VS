namespace SpecFlow.VisualStudio.SpecFlowConnector.Tests;

[UseReporter /*(typeof(VisualStudioReporter))*/]
[UseApprovalSubdirectory("ApprovalTestData\\DumpException")]
public class DumpExceptionTests
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

    public DumpExceptionTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Theory]
    [LabeledMemberData(nameof(TestData))]
    public void Approval(LabeledTestData<Exception> @case)
    {
        NamerFactory.AdditionalInformation = @case.Label;

        var dump = @case.Data.Dump();

        _testOutputHelper.ApprovalsVerify(dump, XunitExtensions.StackTraceScrubber);
    }
}
