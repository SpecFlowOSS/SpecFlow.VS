using System.Diagnostics;

namespace Deveroom.VisualStudio.Diagonostics
{
    public interface IDeveroomLogger
    {
        TraceLevel Level { get; }
        void Log(TraceLevel messageLevel, string message);
    }
}
