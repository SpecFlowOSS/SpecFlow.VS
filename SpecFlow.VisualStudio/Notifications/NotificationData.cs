using System;
using System.Linq;

namespace SpecFlow.VisualStudio.Notifications;

public class NotificationData
{
    public string Id { get; set; }
    public string Message { get; set; }
    public string LinkText { get; set; }
    public string LinkUrl { get; set; }
}
