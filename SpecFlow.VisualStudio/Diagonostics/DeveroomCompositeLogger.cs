using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace SpecFlow.VisualStudio.Diagonostics
{
    public class DeveroomCompositeLogger : IDeveroomLogger, IEnumerable<IDeveroomLogger>
    {
        private IDeveroomLogger[] _loggers = { new DeveroomNullLogger() };

        public TraceLevel Level => _loggers.Max(l => l.Level);
        public void Log(TraceLevel messageLevel, string message)
        {
            foreach (var logger in _loggers)
            {
                logger.Log(messageLevel, message);
            }
        }

        public void Add(IDeveroomLogger logger)
        {
            _loggers = _loggers.Concat(new[] {logger}).ToArray();
        }

        public IEnumerator<IDeveroomLogger> GetEnumerator() => _loggers.AsEnumerable().GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}