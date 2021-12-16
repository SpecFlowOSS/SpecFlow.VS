using System;
using System.Diagnostics;
using SpecFlow.VisualStudio.ProjectSystem;

namespace SpecFlow.VisualStudio.Diagnostics;

public class OutputWindowPaneLogger : IDeveroomLogger
{
    private readonly IDeveroomOutputPaneServices _outputPaneServices;

    public OutputWindowPaneLogger(IDeveroomOutputPaneServices outputPaneServices)
    {
        _outputPaneServices = outputPaneServices;
    }

    public TraceLevel Level { get; set; } = TraceLevel.Info;

    public void Log(TraceLevel messageLevel, string message)
    {
        if (messageLevel <= Level) WriteToOutputPane(messageLevel, message);
    }

    private void WriteToOutputPane(TraceLevel messageLevel, string message)
    {
        _outputPaneServices.SendWriteLine($"{messageLevel}: {message}");
        if (messageLevel <= TraceLevel.Warning)
            _outputPaneServices.Activate();
    }
}
