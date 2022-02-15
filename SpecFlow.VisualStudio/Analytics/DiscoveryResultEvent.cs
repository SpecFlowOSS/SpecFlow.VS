namespace SpecFlow.VisualStudio.Analytics;

public record DiscoveryResultEvent : GenericEvent
{
    public DiscoveryResultEvent(ConnectorResult discoveryResult)
        : base("DiscoveryResult",
            discoveryResult.AnalyticsProperties
                .Union(discoveryResult.ErrorMessage is null
                    ? ImmutableDictionary<string, object>.Empty
                    : new Dictionary<string, object> {["Error"] = discoveryResult.ErrorMessage}))
    {
    }
}
