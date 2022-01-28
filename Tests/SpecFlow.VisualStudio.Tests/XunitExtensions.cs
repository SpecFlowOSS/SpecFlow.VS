// ReSharper disable once CheckNamespace

namespace Xunit.Abstractions;

public static class XunitExtensions
{
    private static string StackTraceReplacement =>
        new StringBuilder()
            .AppendLine()
            .AppendLine("  at ... in ...")
            .AppendLine("  .")
            .AppendLine("  .")
            .AppendLine("  .")
            .AppendLine("  at ... in ...")
            .ToString();

    public static string StackTraceScrubber(string content) => Regex.Replace(content,
        @"StackTrace of (?<exceptionName>.*):\r\n( +at .* in .*\r\n)+",
        $"StackTrace of ${{exceptionName}}:{StackTraceReplacement}");

    public static void ApprovalsVerify(this ITestOutputHelper testOutputHelper, object value)
    {
        Approvals.Verify(value.ToString(), s =>
        {
            testOutputHelper.WriteLine(s);
            return s;
        });
    }

    public static void ApprovalsVerify(this ITestOutputHelper testOutputHelper, object value,
        Func<string, string> scrubber)
    {
        Approvals.Verify(value.ToString(), s =>
        {
            testOutputHelper.WriteLine("--------------------------------");
            testOutputHelper.WriteLine(s);
            testOutputHelper.WriteLine(">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>");
            s = scrubber(s);
            testOutputHelper.WriteLine(s);
            return s;
        });
    }
}
