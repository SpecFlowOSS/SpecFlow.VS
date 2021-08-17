using System;
using System.Linq;
using Moq;
using SpecFlow.VisualStudio.Analytics;
using Xunit;

namespace SpecFlow.VisualStudio.Tests.Analytics
{
    public class AnalyticsTransmitterTests
    {
        Mock<IAnalyticsTransmitterSink> analyticsTransmitterSinkStub;
        Mock<IEnableAnalyticsChecker> enableAnalyticsCheckerStub;

        [Fact]
        public void Should_NotSendAnalytics_WhenDisabled()
        {
            var sut = CreateSut();
            GivenAnalyticsDisabled();

            sut.TransmitEvent(It.IsAny<IAnalyticsEvent>());

            enableAnalyticsCheckerStub.Verify(analyticsChecker => analyticsChecker.IsEnabled(), Times.Once);
            analyticsTransmitterSinkStub.Verify(sink => sink.TransmitEvent(It.IsAny<IAnalyticsEvent>()), Times.Never);
        }

        [Fact]
        public void Should_SendAnalytics_WhenEnabled()
        {
            var sut = CreateSut();
            GivenAnalyticsEnabled();

            sut.TransmitEvent(It.IsAny<IAnalyticsEvent>());

            enableAnalyticsCheckerStub.Verify(analyticsChecker => analyticsChecker.IsEnabled(), Times.Once);
            analyticsTransmitterSinkStub.Verify(sink => sink.TransmitEvent(It.IsAny<IAnalyticsEvent>()), Times.Once);
        }

        [Theory]
        [InlineData("Extension loaded")]
        [InlineData("Extension installed")]
        [InlineData("100 day usage")]
        public void Should_TransmitEvents(string eventName)
        {
            var sut = CreateSut();
            GivenAnalyticsEnabled();

            sut.TransmitEvent(new GenericEvent(eventName));

            analyticsTransmitterSinkStub.Verify(sink => sink.TransmitEvent(It.Is<IAnalyticsEvent>(ae => ae.EventName == eventName)), Times.Once);
        }

        private void GivenAnalyticsEnabled()
        {
            enableAnalyticsCheckerStub.Setup(analyticsChecker => analyticsChecker.IsEnabled()).Returns(true);
        }

        private void GivenAnalyticsDisabled()
        {
            enableAnalyticsCheckerStub.Setup(analyticsChecker => analyticsChecker.IsEnabled()).Returns(false);
        }

        public AnalyticsTransmitter CreateSut()
        {
            analyticsTransmitterSinkStub = new Mock<IAnalyticsTransmitterSink>();
            enableAnalyticsCheckerStub = new Mock<IEnableAnalyticsChecker>();
            return new AnalyticsTransmitter(analyticsTransmitterSinkStub.Object, enableAnalyticsCheckerStub.Object);
        }
    }
}
