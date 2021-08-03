using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SpecFlow.VisualStudio.Diagonostics;

namespace SpecFlow.VisualStudio.VsxStubs.ProjectSystem
{
    public class StubLogger : IDeveroomLogger
    {
        public List<Tuple<TraceLevel, string>> Messages { get; } = new List<Tuple<TraceLevel, string>>();
        public TraceLevel Level { get; } = TraceLevel.Verbose;
        public void Log(TraceLevel messageLevel, string message)
        {
            Messages.Add(new Tuple<TraceLevel, string>(messageLevel, message));
        }
    }
}
