using System;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.VisualStudio.Shell;
using SpecFlow.VisualStudio.Monitoring;
using SpecFlow.VisualStudio.ProjectSystem;

namespace SpecFlow.VisualStudio.Notifications
{
    public class NotificationInfoBarFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly NotificationDataStore _notificationDataStore;
        private readonly ExternalBrowserNotificationService _browserNotificationService;
        private readonly IMonitoringService _monitoringService;
        
        public NotificationInfoBarFactory([Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider, IIdeScope ideScope, NotificationDataStore notificationDataStore, IMonitoringService monitoringService)
        {
            _serviceProvider = serviceProvider;
            _notificationDataStore = notificationDataStore;
            _browserNotificationService = new ExternalBrowserNotificationService(ideScope);
            _monitoringService = monitoringService;
        }

        public NotificationInfoBar Create(NotificationData notification)
        {
            return new NotificationInfoBar(_serviceProvider, _browserNotificationService, _notificationDataStore, _monitoringService, notification);
        }
    }
}
