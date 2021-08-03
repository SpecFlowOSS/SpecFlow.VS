using System.Diagnostics;

namespace SpecFlow.VisualStudio.Diagonostics
{
    public interface IDeveroomLogger
    {
        TraceLevel Level { get; }
        void Log(TraceLevel messageLevel, string message);
    }
}
