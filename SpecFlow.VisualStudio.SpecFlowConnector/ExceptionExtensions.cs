namespace SpecFlow.VisualStudio.SpecFlowConnector;

public static class ExceptionExtensions
{
    public static string Dump(this Exception ex) =>
        ex.AsEnumerable()
            .Select(e => (
                message: FormatMessage(e),
                fullName: e.GetType().FullName ?? "<Unknown>",
                stackTrace: e.StackTrace is null
                    ? string.Empty
                    : $"StackTrace of {e.GetType().Name}:{Environment.NewLine}{e.StackTrace}"
            ))
            .Aggregate((accumulate, current) => (
                message: $"{accumulate.message} -> {current.message}",
                fullName: $"{accumulate.fullName}->{current.fullName}",
                stackTrace:
                $"{current.stackTrace}{(string.IsNullOrWhiteSpace(accumulate.stackTrace) ? string.Empty : Environment.NewLine + accumulate.stackTrace)}")
            )
            .Map(ToTextBlocks)
            .Where(block => !string.IsNullOrWhiteSpace(block))
            .Map(blocks => new StringBuilder().AppendLines(blocks))
            .ToString()
            .Map(s => s.TrimEnd('\r', '\n'));

    private static string FormatMessage(Exception e)
    {
        var splitIndex = e.Message.IndexOf(" (", StringComparison.Ordinal);
        return splitIndex < 0
            ? e.Message
            : e.Message.Substring(0, splitIndex);
    }

    public static IEnumerable<Exception> AsEnumerable(this Exception? ex)
    {
        while (ex != null)
        {
            yield return ex;
            switch (ex)
            {
                case AggregateException aggregateException:
                    foreach (var e in aggregateException.InnerExceptions.SelectMany(AsEnumerable)) yield return e;
                    yield break;
                default:
                    ex = ex.InnerException;
                    break;
            }
        }
    }

    private static IEnumerable<string> ToTextBlocks((string, string, string) result)
    {
        var (messages, fullNames, stackTraces) = result;
        yield return $"Error: {messages}";
        yield return $"Exception: {fullNames}";
        if (string.IsNullOrWhiteSpace(stackTraces)) yield break;
        yield return stackTraces;
    }
}
