using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using SpecFlow.VisualStudio.Common;
using SpecFlow.VisualStudio.EventTracking;

namespace SpecFlow.VisualStudio.Analytics
{
    public interface IAnalyticsTransmitter
    {
        void TransmitEvent(IAnalyticsEvent runtimeEvent);
        void TransmitExceptionEvent(Exception exception, bool? isFatal = null, bool anonymize = true);
        void TransmitExceptionEvent(Exception exception, Dictionary<string, string> additionalProps, bool? isFatal = null, bool anonymize = true);
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
                TransmitExceptionEvent(ex, anonymize: false);
            }
        }

        public void TransmitExceptionEvent(Exception exception, bool? isFatal = null, bool anonymize = true)
        {
            var isNormalError = IsNormalError(exception);
            TransmitException(exception.Message, isFatal ?? !isNormalError, exception, anonymize: anonymize);
        }

        public void TransmitExceptionEvent(Exception exception, Dictionary<string, string> additionalProps, bool? isFatal = null, bool anonymize = true)
        {
            TransmitException(exception.Message, isFatal ?? false, exception, additionalProps, anonymize);
        }
        
        private void TransmitException(string errorMessage, bool isFatal, Exception exception = null, Dictionary<string, string> additionalProps = null, bool anonymize = true)
        {
            try
            {
                if (anonymize)
                {
                    errorMessage = exception != null ? ErrorAnonymizer.AnonymizeException(exception) : 
                        ErrorAnonymizer.AnonymizeErrorMessage(errorMessage);
                }

                var exceptionAnalyticsEvent = new GenericEvent("Visual Studio Extension Exception",
                    new Dictionary<string, string>
                    {
                        { "ExceptionDetail", errorMessage },
                        { "IsFatal", isFatal.ToString() }
                    }
                );
                if (exception != null)
                {
                    exceptionAnalyticsEvent.Properties.Add("ExceptionType", exception.GetType().ToString());
                    exceptionAnalyticsEvent.Properties.Add("ExceptionStackTrace", exception.StackTrace);
                }

                if (additionalProps != null)
                {
                    foreach (var prop in additionalProps)
                    {
                        exceptionAnalyticsEvent.Properties.Add(prop.Key, prop.Value);
                    }
                }
                
                _analyticsTransmitterSink.TransmitException(exception, additionalProps);
            }
            catch (Exception)
            {
                // catch all exceptions since we do not want to break the whole extension simply because data transmission failed
            }
        }

        private static bool IsNormalError(Exception exception)
        {
            if (exception is AggregateException aggregateException)
                return aggregateException.InnerExceptions.All(IsNormalError);
            return
                exception is DeveroomConfigurationException ||
                exception is TimeoutException ||
                exception is TaskCanceledException ||
                exception is OperationCanceledException ||
                exception is HttpRequestException;
        }
    }
}
