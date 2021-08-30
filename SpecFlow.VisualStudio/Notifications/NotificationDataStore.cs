using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace SpecFlow.VisualStudio.Notifications
{
    public class NotificationDataStore
    {
        public static readonly string NotificationFilePath = Environment.ExpandEnvironmentVariables(@"%APPDATA%\SpecFlow\notification_vs");

        public bool IsDismissed(NotificationData notification)
        {
            try
            {
                var text = File.ReadAllText(NotificationFilePath, Encoding.UTF8);
                if (text == notification.Id) return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex, "Error during fetching notification data.");
            }

            return false;
        }

        public void SetDismissed(NotificationData notification)
        {
            try
            {
                File.WriteAllText(NotificationFilePath, notification.Id, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex, "Error during saving notification data.");
            }
        }
    }
}
