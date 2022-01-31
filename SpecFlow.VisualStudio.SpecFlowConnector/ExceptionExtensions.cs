// ReSharper disable once CheckNamespace

namespace System;

public static class ExceptionExtensions
{
    public static string Dump(this Exception ex) =>
        ex.AsEnumerable()
            .Select(e => (
                message: e.Message,
                fullName: e.GetType().FullName ?? "<Unknown>",
                stackTrace: $"StackTrace of {e.GetType().Name}:{Environment.NewLine}{e.StackTrace}"
            ))
            .Aggregate((accumulate, current) => (
                message: $"{accumulate.message} -> {current.message}",
                fullName: $"{accumulate.fullName}->{current.fullName}",
                stackTrace: $"{current.stackTrace}{Environment.NewLine}{accumulate.stackTrace}")
            )
            .Map(ToTextBlocks)
            .Map(block => new StringBuilder().AppendLines(block))
            .ToString();

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
        yield return stackTraces;
    }
}
