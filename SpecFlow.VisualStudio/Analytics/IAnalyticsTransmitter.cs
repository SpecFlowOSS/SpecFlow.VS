#nullable enable
namespace SpecFlow.VisualStudio.Analytics;

public interface IAnalyticsTransmitter
{
    void TransmitEvent(IAnalyticsEvent runtimeEvent);
    void TransmitExceptionEvent(Exception exception, IEnumerable<KeyValuePair<string, object>> additionalProps);
    void TransmitFatalExceptionEvent(Exception exception, bool isFatal);
}
