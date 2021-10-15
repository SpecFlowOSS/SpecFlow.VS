using System.ComponentModel.Composition;

namespace SpecFlow.VisualStudio.VsxStubs.ProjectSystem;

[Export(typeof(IAnalyticsEvent))]
[DebuggerDisplay("{EventName}")]
public class StubAnalyticsEvent : IAnalyticsEvent
{
    public StubAnalyticsEvent(string eventName, Dictionary<string, object> properties = null)
    {
        EventName = eventName;
        Properties = properties ?? new Dictionary<string, object>();
    }

    public string EventName { get; }
    public Dictionary<string, object> Properties { get; }
}
