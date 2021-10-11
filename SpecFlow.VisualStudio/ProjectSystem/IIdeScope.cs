using System;
using System.IO.Abstractions;
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

        IPersistentSpan CreatePersistentTrackingPosition(SourceLocation sourceLocation);
        IProjectScope[] GetProjectsWithFeatureFiles();

        IDisposable CreateUndoContext(string undoLabel);
        bool GetTextBuffer(SourceLocation sourceLocation, out ITextBuffer textBuffer);
        SyntaxTree GetSyntaxTree(ITextBuffer textBuffer);
        void RunOnUiThread(Action action);
        void OpenIfNotOpened(string path);
    }
}
