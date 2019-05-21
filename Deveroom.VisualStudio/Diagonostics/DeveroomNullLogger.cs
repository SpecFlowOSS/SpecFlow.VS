using System.Diagnostics;

namespace Deveroom.VisualStudio.Diagonostics
{
    public class DeveroomNullLogger : IDeveroomLogger
    {
        public TraceLevel Level { get; } = TraceLevel.Off;
        public void Log(TraceLevel messageLevel, string message)
        {
            //nop;
        }
    }
}