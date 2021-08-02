using System;
using Interlocked = System.Threading.Interlocked;

namespace SpecFlow.VisualStudio
{
    public class CalculationCache<T> where T : class
    {
        private static readonly object Invalid = new object();
        private static readonly object Valid = new object();
        private static readonly object Calculating = new object();

        private object _state = Invalid;
        public T Value { get; private set; }

        public bool IsInvalid => _state == Invalid;
        public bool IsValid => _state == Valid;
        public bool IsCalculating => _state == Calculating;

        public void Invalidate(bool clearValue = false)
        {
            Invalidate(clearValue, out _);
        }

        public void Invalidate(bool clearValue, out bool calculationInProgress)
        {
            var previousState = Interlocked.Exchange(ref _state, Invalid);
            if (clearValue)
                Value = null;

            calculationInProgress = previousState == Calculating;
        }

        public bool ReCalculate(Func<T> calculateValue)
        {
            Invalidate(false, out var calculationInProgress);
            if (calculationInProgress)
                return false;

            bool setAsValid = false;
            do
            {
                if (!TryStartCalculation())
                    return false; // there is another calculation started in the meanwhile
                var newValue = calculateValue();
                setAsValid = SetCalculationResult(newValue);
            } while (!setAsValid);

            return true;
        }

        private bool TryStartCalculation()
        {
            var result = Interlocked.CompareExchange(ref _state, Calculating, Invalid);
            return result == Invalid;
        }

        private bool SetCalculationResult(T value)
        {
            Value = value;
            var result = Interlocked.CompareExchange(ref _state, Valid, Calculating);
            return result == Calculating;
        }
    }
}
