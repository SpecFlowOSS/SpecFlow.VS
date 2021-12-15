using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using SpecFlow.VisualStudio.Diagnostics;
using SpecFlow.VisualStudio.Discovery;
using SpecFlow.VisualStudio.Monitoring;
using SpecFlow.VisualStudio.ProjectSystem;
using SpecFlow.VisualStudio.ProjectSystem.Actions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Moq;
using SpecFlow.VisualStudio.Editor.Services;
using Xunit.Abstractions;

namespace SpecFlow.VisualStudio.VsxStubs.ProjectSystem
{
    public class StubIdeScope : Mock<IIdeScope>, IIdeScope
    {
        public StubAnalyticsTransmitter AnalyticsTransmitter { get; }
        public IDictionary<string, StubWpfTextView> OpenViews { get; } = new Dictionary<string, StubWpfTextView>();
        public StubLogger StubLogger { get; } = new StubLogger();
        public DeveroomCompositeLogger CompositeLogger { get; } = new DeveroomCompositeLogger
        {
            new DeveroomDebugLogger(TraceLevel.Verbose)
        };

        public bool IsSolutionLoaded { get; } = true;

        public IProjectScope GetProject(ITextBuffer textBuffer)
        {
            return textBuffer.Properties.GetProperty<IProjectScope>(typeof(IProjectScope));
        }

        public IDeveroomLogger Logger => CompositeLogger;
        public IIdeActions Actions { get; set; }
        public IDeveroomWindowManager WindowManager => StubWindowManager;
        public IFileSystem FileSystem { get; private set; } = new MockFileSystem();
        public IDeveroomOutputPaneServices DeveroomOutputPaneServices { get; } = null;
        public IDeveroomErrorListServices DeveroomErrorListServices { get; } = new StubErrorListServices();
        public StubWindowManager StubWindowManager { get; } = new StubWindowManager();
        public List<IProjectScope> ProjectScopes { get; } = new List<IProjectScope>();
        public IMonitoringService MonitoringService { get; }
        public IWpfTextView CurrentTextView { get; internal set; }

        public event EventHandler<EventArgs> WeakProjectsBuilt;
        public event EventHandler<EventArgs> WeakProjectOutputsUpdated;

        public void CalculateSourceLocationTrackingPositions(IEnumerable<SourceLocation> sourceLocations)
        {
        }

        public StubWpfTextView CreateTextView(TestText inputText, string newLine = null, IProjectScope projectScope = null, string contentType = VsContentTypes.FeatureFile, string filePath = null)
        {
            if (filePath != null && !Path.IsPathRooted(filePath) && projectScope != null)
                filePath = Path.Combine(projectScope.ProjectFolder, filePath);

            if (projectScope == null && filePath != null)
            {
                projectScope = ProjectScopes.FirstOrDefault(p =>
                    (p as InMemoryStubProjectScope)?.FilesAdded.Any(f => f.Key == filePath) ?? false);
            }

            var textView = StubWpfTextView.CreateTextView(this, inputText, newLine, projectScope, contentType, filePath);
            if (filePath != null)
                OpenViews[filePath] = textView;

            CurrentTextView = textView;

            return textView;
        }

        public bool GetTextBuffer(SourceLocation sourceLocation, out ITextBuffer textBuffer)
        {
            if (OpenViews.TryGetValue(sourceLocation.SourceFile, out var view))
            {
                textBuffer =view.TextBuffer;
                return true;
            }

            textBuffer = default;
            return false;
        }

        public IWpfTextView EnsureOpenTextView(SourceLocation sourceLocation)
        {
            if (OpenViews.TryGetValue(sourceLocation.SourceFile, out var view))
                return view;

            var lines = FileSystem.File.ReadAllLines(sourceLocation.SourceFile);
            var textView = CreateTextView(new TestText(lines), filePath: sourceLocation.SourceFile);
            return textView;
        }

        public SyntaxTree GetSyntaxTree(ITextBuffer textBuffer)
        {
            var fileContent = textBuffer.CurrentSnapshot.GetText();
            return CSharpSyntaxTree.ParseText(fileContent);
        }

        public Task RunOnBackgroundThread(Func<Task> action, Action<Exception> onException, [CallerMemberName] string callerName = "???")
            => Object.RunOnBackgroundThread(action, onException, callerName);

        public void SynchronizeRunOnBackgroundThread()
        {
            Setup(s => s.RunOnBackgroundThread(It.IsAny<Func<Task>>(), It.IsAny<Action<Exception>>(),
                    It.IsAny<string>()))
                .Returns(async (Func<Task> action, Action<Exception> onException, string callerName) =>
                {
                    try
                    {
                        await action();
                    }
                    catch (Exception e)
                    {
                        Logger.LogException(MonitoringService, e);
                        onException(e);
                    }
                });
        }

        public void UnSynchronizeRunOnBackgroundThread()
        {
            Setup(s => s.RunOnBackgroundThread(It.IsAny<Func<Task>>(), It.IsAny<Action<Exception>>(),
                    It.IsAny<string>()))
                .Returns((Func<Task> action, Action<Exception> onException, string callerName) =>
                {
                    StackTrace stackTraceSnapshot = new StackTrace();
                    return Task.Run(async () =>
                    {
                        try
                        {
                            await action();
                        }
                        catch (Exception e)
                        {
                            Logger.LogException(MonitoringService, e, $"Called from {callerName}. {stackTraceSnapshot}");
                            onException(e);
                        }
                    });
                });
        }

        public Task RunOnUiThread(Action action)
        {
            action();
            return Task.CompletedTask;
        }

        public void OpenIfNotOpened(string path)
        {
            if (OpenViews.TryGetValue(path, out _))
                return;

            var lines = FileSystem.File.ReadAllLines(path);
            CreateTextView(new TestText(lines), filePath: path);
        }

        public IProjectScope[] GetProjectsWithFeatureFiles()
        {
            return ProjectScopes.ToArray();
        }

        public IDisposable CreateUndoContext(string undoLabel)
        {
            return null;
        }

        public StubIdeScope(ITestOutputHelper testOutputHelper)
        {
            AnalyticsTransmitter = new StubAnalyticsTransmitter(Logger);
            MonitoringService = 
                new MonitoringService(
                    AnalyticsTransmitter, 
                    new Mock<IWelcomeService>().Object, 
                    new Mock<ITelemetryConfigurationHolder>().Object);

            CompositeLogger.Add(new DeveroomXUnitLogger(testOutputHelper));
            CompositeLogger.Add(StubLogger);
            Actions = new StubIdeActions(this);
            VsxStubObjects.Initialize();

            UnSynchronizeRunOnBackgroundThread();
        }

        public void TriggerProjectsBuilt()
        {
            WeakProjectsBuilt?.Invoke(this, EventArgs.Empty);
            WeakProjectOutputsUpdated?.Invoke(this, EventArgs.Empty);
        }

        public void UsePhysicalFileSystem()
        {
            FileSystem = new FileSystem();
        }

    }
}
