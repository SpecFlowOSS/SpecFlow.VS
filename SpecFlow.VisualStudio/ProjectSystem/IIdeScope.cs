using System;
using System.IO.Abstractions;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using SpecFlow.VisualStudio.Diagnostics;
using SpecFlow.VisualStudio.Discovery;
using SpecFlow.VisualStudio.Monitoring;
using SpecFlow.VisualStudio.ProjectSystem.Actions;
using Microsoft.VisualStudio.Text;

namespace SpecFlow.VisualStudio.ProjectSystem
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
        IDeveroomErrorListServices DeveroomErrorListServices { get; }
        IFileSystem FileSystem { get; }
        event EventHandler<EventArgs> WeakProjectsBuilt;
        event EventHandler<EventArgs> WeakProjectOutputsUpdated;

        void CalculateSourceLocationTrackingPositions(IEnumerable<SourceLocation> sourceLocations);
        IProjectScope[] GetProjectsWithFeatureFiles();

        IDisposable CreateUndoContext(string undoLabel);
        bool GetTextBuffer(SourceLocation sourceLocation, out ITextBuffer textBuffer);
        SyntaxTree GetSyntaxTree(ITextBuffer textBuffer);
        Task RunOnBackgroundThread(Func<Task> action, Action<Exception> onException, [CallerMemberName] string callerName = "???");
        Task RunOnUiThread(Action action);
        void OpenIfNotOpened(string path);
    }
}
