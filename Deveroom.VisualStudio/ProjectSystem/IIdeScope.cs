using System;
using System.IO.Abstractions;
using Deveroom.VisualStudio.Diagonostics;
using Deveroom.VisualStudio.Discovery;
using Deveroom.VisualStudio.Monitoring;
using Deveroom.VisualStudio.ProjectSystem.Actions;
using Microsoft.VisualStudio.Text;

namespace Deveroom.VisualStudio.ProjectSystem
{
    public interface IIdeScope
    {
        bool IsSolutionLoaded { get; }
        IProjectScope GetProject(ITextBuffer textBuffer);
        IDeveroomLogger Logger { get; }
        IMonitoringService MonitoringService { get; }
        IIdeActions Actions { get; }
        IDeveroomWindowManager WindowManager { get; }
        IDeveroomOutputPaneServices DeveroomOutputPaneServices { get; }
        IFileSystem FileSystem { get; }
        event EventHandler<EventArgs> WeakProjectsBuilt;
        event EventHandler<EventArgs> WeakProjectOutputsUpdated;

        IPersistentSpan CreatePersistentTrackingPosition(SourceLocation sourceLocation);
        IProjectScope[] GetProjectsWithFeatureFiles();
    }
}
