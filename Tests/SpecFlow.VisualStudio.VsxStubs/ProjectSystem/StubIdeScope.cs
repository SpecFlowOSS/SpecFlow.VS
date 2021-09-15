using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using SpecFlow.VisualStudio.Diagnostics;
using SpecFlow.VisualStudio.Discovery;
using SpecFlow.VisualStudio.Monitoring;
using SpecFlow.VisualStudio.ProjectSystem;
using SpecFlow.VisualStudio.ProjectSystem.Actions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.TextManager.Interop;
using Moq;
using Xunit.Abstractions;

namespace SpecFlow.VisualStudio.VsxStubs.ProjectSystem
{
    public class StubIdeScope : IIdeScope
    {
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
        public IDeveroomErrorListServices DeveroomErrorListServices { get; } = null;
        public StubWindowManager StubWindowManager { get; } = new StubWindowManager();
        public List<IProjectScope> ProjectScopes { get; } = new List<IProjectScope>();
        public IMonitoringService MonitoringService { get; }

        public event EventHandler<EventArgs> WeakProjectsBuilt;
        public event EventHandler<EventArgs> WeakProjectOutputsUpdated;

        public IPersistentSpan CreatePersistentTrackingPosition(SourceLocation sourceLocation)
        {
            return null;
        }

        public StubWpfTextView CreateTextView(TestText inputText, string newLine = null, IProjectScope projectScope = null, string contentType = "deveroom", string filePath = null)
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
            return textView;
        }

        public ITextBuffer GetTextBuffer(SourceLocation sourceLocation)
        {
            var lines = FileSystem.File.ReadAllLines(sourceLocation.SourceFile);
            var textView = CreateTextView(new TestText(lines), filePath: sourceLocation.SourceFile);
            return textView.TextBuffer;
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
            MonitoringService = new Mock<IMonitoringService>().Object;
            CompositeLogger.Add(new DeveroomXUnitLogger(testOutputHelper));
            CompositeLogger.Add(StubLogger);
            Actions = new StubIdeActions(this);
            VsxStubObjects.Initialize();
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

        class ActionsSetter : IDisposable
        {
            private readonly StubIdeScope _ideScope;
            private readonly IIdeActions _originalActions;

            public ActionsSetter(StubIdeScope ideScope, IIdeActions actions)
            {
                _ideScope = ideScope;
                _originalActions = _ideScope.Actions;
                _ideScope.Actions = actions;
            }

            public void Dispose()
            {
                _ideScope.Actions = _originalActions;
            }
        }

        public IDisposable SetActions(IIdeActions actions)
        {
            return new ActionsSetter(this, actions);
        }
    }
}
