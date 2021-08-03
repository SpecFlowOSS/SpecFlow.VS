using System;
using System.Diagnostics;
using System.Windows.Threading;

namespace SpecFlow.VisualStudio.Editor.Services
{
    public class ActionThrottler
    {
        private const bool EnableDebugTrace = false;
        private readonly DispatcherTimer _dispatcherTimer;
        private readonly Stopwatch _changeFrequencyMeasurer = new Stopwatch();
        private readonly Action _action;
        private readonly int _typingStartDetectionMilliseconds;

        public ActionThrottler(Action action, int delayMilliseconds = 300, int typingStartDetectionMilliseconds = 500)
        {
            _action = action;
            _typingStartDetectionMilliseconds = typingStartDetectionMilliseconds;
            _dispatcherTimer = new DispatcherTimer(DispatcherPriority.ApplicationIdle)
            {
                Interval = TimeSpan.FromMilliseconds(delayMilliseconds)
            };
            _dispatcherTimer.Tick += (sender, args) =>
            {
                _dispatcherTimer.Stop();
                Debug.WriteLineIf(EnableDebugTrace, "Delayed action", "ActionThrottler");
                _action();
            };
        }

        public void TriggerAction(bool forceDelayed = false, bool forceDirect = false)
        {
            _dispatcherTimer.Stop();
            var typingStart = !_changeFrequencyMeasurer.IsRunning || _changeFrequencyMeasurer.ElapsedMilliseconds >= _typingStartDetectionMilliseconds;
            _changeFrequencyMeasurer.Restart();

            Debug.WriteLineIf(EnableDebugTrace, $"Action triggered, typingStart:{typingStart}", "ActionThrottler");

            if (forceDirect || (typingStart && !forceDelayed))
            {
                Debug.WriteLineIf(EnableDebugTrace, "Direct action", "ActionThrottler");
                _action();
            }
            else
            {
                _dispatcherTimer.Start();
            }
        }
    }
}