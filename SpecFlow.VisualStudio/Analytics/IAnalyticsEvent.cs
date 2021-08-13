using System.Collections.Generic;
using System.ComponentModel.Composition;

namespace SpecFlow.VisualStudio.Analytics
{
    public interface IAnalyticsEvent
    {
        string EventName { get; }

        Dictionary<string, string> Properties { get; }
    }

    [Export(typeof(IAnalyticsEvent))]
    public class GenericEvent : IAnalyticsEvent
    {
        public GenericEvent(string eventName, Dictionary<string, string> properties = null)
        {
            EventName = eventName;
            Properties = properties ?? new Dictionary<string, string>();
        }

        public string EventName { get; }
        public Dictionary<string, string> Properties { get; }
    }
}
