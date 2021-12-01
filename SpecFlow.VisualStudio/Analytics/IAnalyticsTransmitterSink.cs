namespace SpecFlow.VisualStudio.Analytics;

public interface IAnalyticsTransmitterSink
{
    void TransmitEvent(IAnalyticsEvent analyticsEvent);
    void TransmitException(Exception exception, Dictionary<string, object> eventName);
}

[System.Composition.Export(typeof(IAnalyticsTransmitterSink))]
public class AppInsightsAnalyticsTransmitterSink : IAnalyticsTransmitterSink
{
    private readonly IEnableAnalyticsChecker _enableAnalyticsChecker;
    private readonly Lazy<TelemetryClient> _telemetryClient;

    [System.Composition.ImportingConstructor]
    public AppInsightsAnalyticsTransmitterSink(IEnableAnalyticsChecker enableAnalyticsChecker,
        IUserUniqueIdStore userUniqueIdStore)
    {
        _enableAnalyticsChecker = enableAnalyticsChecker;
        _telemetryClient = new Lazy<TelemetryClient>(() => GetTelemetryClient(userUniqueIdStore.GetUserId()));
    }

    public void TransmitEvent(IAnalyticsEvent analyticsEvent)
    {
        if (!_enableAnalyticsChecker.IsEnabled())
            return;

        var appInsightsEvent = new EventTelemetry(analyticsEvent.EventName)
        {
            Timestamp = DateTime.UtcNow
        };

        AddProps(appInsightsEvent, analyticsEvent.Properties);

        TrackTelemetry(appInsightsEvent);
    }

    public void TransmitException(Exception exception, Dictionary<string, object> additionalProps)
    {
        if (!_enableAnalyticsChecker.IsEnabled())
            return;

        var exceptionTelemetry = new ExceptionTelemetry(exception)
        {
            Timestamp = DateTime.UtcNow
        };

        AddProps(exceptionTelemetry, additionalProps);

        TrackTelemetry(exceptionTelemetry);
    }

    private void AddProps(ISupportProperties telemetry, Dictionary<string, object> additionalProps)
    {
        foreach (var prop in additionalProps) telemetry.Properties.Add(prop.Key, prop.Value.ToString());
    }

    private void TrackTelemetry(ITelemetry telemetry)
    {
        switch (telemetry)
        {
            case EventTelemetry eventTelemetry:
                _telemetryClient.Value.TrackEvent(eventTelemetry);
                break;
            case ExceptionTelemetry exceptionTelemetry:
                _telemetryClient.Value.TrackException(exceptionTelemetry);
                break;
        }

        _telemetryClient.Value.Flush();
    }

    private TelemetryClient GetTelemetryClient(string userUniqueId)
    {
        return new TelemetryClient
        {
            Context =
            {
                User =
                {
                    Id = userUniqueId,
                    AccountId = userUniqueId
                }
            }
        };
    }
}
