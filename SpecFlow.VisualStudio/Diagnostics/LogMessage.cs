namespace SpecFlow.VisualStudio.Diagnostics;

[DebuggerDisplay("{TimeStamp} {CallerMethod} {Message}")]
public record LogMessage(
    TraceLevel Level,
    string Message,
    DateTimeOffset TimeStamp,
    string CallerMethod,
    [CanBeNull] Exception Exception = default!);
