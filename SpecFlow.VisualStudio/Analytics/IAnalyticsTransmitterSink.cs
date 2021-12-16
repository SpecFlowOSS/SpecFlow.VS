#nullable enable
namespace SpecFlow.VisualStudio.Analytics;

public interface IAnalyticsTransmitterSink
{
    void TransmitEvent(IAnalyticsEvent analyticsEvent);
    void TransmitException(Exception exception, IEnumerable<KeyValuePair<string, object>> eventName);
}
