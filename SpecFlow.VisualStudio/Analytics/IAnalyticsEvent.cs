#nullable enable
namespace SpecFlow.VisualStudio.Analytics;

public interface IAnalyticsEvent
{
    string EventName { get; }
    ImmutableDictionary<string, object> Properties { get; }
}
