using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;

namespace SpecFlow.VisualStudio.Analytics
{
    public interface IAnalyticsTransmitter
    {
        void TransmitEvent(IAnalyticsEvent runtimeEvent);
        void TransmitExceptionEvent(Exception exception);
    }

    [Export(typeof(IAnalyticsTransmitter))]
    public class AnalyticsTransmitter : IAnalyticsTransmitter
    {
        private readonly IAnalyticsTransmitterSink _analyticsTransmitterSink;
        private readonly IEnableAnalyticsChecker _enableAnalyticsChecker;

        [ImportingConstructor]
        public AnalyticsTransmitter(IAnalyticsTransmitterSink analyticsTransmitterSink, IEnableAnalyticsChecker enableAnalyticsChecker)
        {
            _analyticsTransmitterSink = analyticsTransmitterSink;
            _enableAnalyticsChecker = enableAnalyticsChecker;
        }

        public void TransmitEvent(IAnalyticsEvent analyticsEvent)
        {
            try
            {
                if (!_enableAnalyticsChecker.IsEnabled())
                {
                    return;
                }
                
                _analyticsTransmitterSink.TransmitEvent(analyticsEvent);
            }
            catch (Exception ex)
            {
                TransmitExceptionEvent(ex);
            }
        }

        public void TransmitExceptionEvent(Exception exception)
        {
            try
            {
                //Visual Studio Extension Exception
                var exceptionAnalyticsEvent = new GenericEvent("Visual Studio Extension Exception",
                    new Dictionary<string, string>
                    {
                        { "ExceptionType", exception.GetType().ToString() }
                    }
                );
                _analyticsTransmitterSink.TransmitEvent(exceptionAnalyticsEvent);
            }
            catch (Exception)
            {
                // catch all exceptions since we do not want to break the whole extension simply because data transmission failed
            }
        }
        
    }
}
