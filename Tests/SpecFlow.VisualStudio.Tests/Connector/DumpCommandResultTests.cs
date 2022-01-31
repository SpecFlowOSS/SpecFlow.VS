﻿namespace SpecFlow.VisualStudio.Tests.Connector;

[UseReporter /*(typeof(VisualStudioReporter))*/]
[UseApprovalSubdirectory("../ApprovalTestData")]
public class DumpCommandResultTests
{
    public static IEnumerable<object[]> Exceptions = new List<object[]>
    {
        new object[] {"SimpleException", new Exception().WithStackTrace()},
        new object[] {"SimpleExceptionWithMessage", new Exception("Message").WithStackTrace()},
        new object[] {"NotImplementedException", new NotImplementedException().WithStackTrace()},
        new object[] {"NotImplementedExceptionWithMessage", new NotImplementedException("Message").WithStackTrace()},
        new object[]
        {
            "WithOneException",
            new Exception("Message1", new NotImplementedException("Message2").WithStackTrace())
        },
        new object[]
        {
            "NestedExceptions",
            new Exception("Message1",
                    new NotImplementedException("Message2"
                            , new InvalidOperationException("Messasge3")
                                .WithStackTrace())
                        .WithStackTrace())
                .WithStackTrace()
        },
        new object[]
        {
            "AggregateException",
            new AggregateException("Message1",
                new NotImplementedException("Message2").WithStackTrace(),
                new InvalidOperationException("Messasge3").WithStackTrace()
            ).WithStackTrace()
        },
        new object[]
        {
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
        }
    };

    private readonly ITestOutputHelper _testOutputHelper;

    public DumpCommandResultTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Theory]
    [MemberData(nameof(Exceptions))]
    public void Approval(string testName, Exception exception)
    {
        NamerFactory.AdditionalInformation = testName;

        var dump = exception.Dump();

        _testOutputHelper.ApprovalsVerify(dump, XunitExtensions.StackTraceScrubber);
    }
}
