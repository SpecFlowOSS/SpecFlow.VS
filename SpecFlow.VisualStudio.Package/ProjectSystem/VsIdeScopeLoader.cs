﻿using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO.Abstractions;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SpecFlow.VisualStudio.Diagnostics;
using SpecFlow.VisualStudio.Discovery;
using SpecFlow.VisualStudio.Monitoring;
using SpecFlow.VisualStudio.ProjectSystem.Actions;
using EnvDTE;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Project = EnvDTE.Project;

namespace SpecFlow.VisualStudio.ProjectSystem
{
    [Export(typeof(IIdeScope))]
    public class VsIdeScopeLoader : IVsIdeScope
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IDeveroomLogger _safeLogger;
        private readonly IMonitoringService _safeMonitoringService;
        private readonly Lazy<IVsIdeScope> _projectSystemReference;
        private IVsIdeScope VsIdeScope => _projectSystemReference.Value;
        private bool IsLoaded => _projectSystemReference.IsValueCreated;

        [ImportingConstructor]
        public VsIdeScopeLoader([Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            // ReSharper disable once RedundantArgumentDefaultValue
            _safeLogger = GetSafeLogger();
            _safeMonitoringService = GetSafeMonitoringService(serviceProvider);
            _projectSystemReference = new Lazy<IVsIdeScope>(LoadProjectSystem, LazyThreadSafetyMode.ExecutionAndPublication);
        }

        private IMonitoringService GetSafeMonitoringService(IServiceProvider serviceProvider)
        {
            try
            {
                var safeMonitoringService = VsUtils.ResolveMefDependency<IMonitoringService>(serviceProvider);
                if (safeMonitoringService != null)
                    _safeLogger.LogVerbose("Monitoring service loaded");
                return safeMonitoringService ?? NullVsIdeScope.GetNullMonitoringService();
            }
            catch
            {
                return NullVsIdeScope.GetNullMonitoringService();
            }
        }

        private static IDeveroomLogger GetSafeLogger()
        {
            try
            {
                return new DeveroomFileLogger();
            }
            catch
            {
                return new DeveroomNullLogger();
            }
        }

        private IVsIdeScope LoadProjectSystem()
        {
            _safeLogger.LogVerbose("Loading VsIdeScope...");
            try
            {
                MonitorLoadProjectSystem();
                return VsUtils.ResolveMefDependency<VsIdeScope>(_serviceProvider);
            }
            catch (Exception ex)
            {
                var nullVsProjectSystem = new NullVsIdeScope(_safeLogger, _serviceProvider, _safeMonitoringService);
                ReportErrorServices.ReportInitError(nullVsProjectSystem, ex);
                return nullVsProjectSystem;
            }
        }

        private void MonitorLoadProjectSystem()
        {
            try
            {
                _safeMonitoringService.MonitorLoadProjectSystem();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        #region Delegaing members

        public bool IsSolutionLoaded => VsIdeScope.IsSolutionLoaded;

        public IProjectScope GetProject(ITextBuffer textBuffer)
        {
            return VsIdeScope.GetProject(textBuffer);
        }

        public IDeveroomLogger Logger => VsIdeScope.Logger;
        public IMonitoringService MonitoringService => VsIdeScope.MonitoringService;

        public IIdeActions Actions => VsIdeScope.Actions;

        public IDeveroomWindowManager WindowManager => VsIdeScope.WindowManager;

        public IFileSystem FileSystem => VsIdeScope.FileSystem;

        public event EventHandler<EventArgs> WeakProjectsBuilt
        {
            add => VsIdeScope.WeakProjectsBuilt += value;
            remove => VsIdeScope.WeakProjectsBuilt -= value;
        }

        public event EventHandler<EventArgs> WeakProjectOutputsUpdated
        {
            add => VsIdeScope.WeakProjectOutputsUpdated += value;
            remove => VsIdeScope.WeakProjectOutputsUpdated -= value;
        }

        public IPersistentSpan CreatePersistentTrackingPosition(SourceLocation sourceLocation)
        {
            return VsIdeScope.CreatePersistentTrackingPosition(sourceLocation);
        }

        public bool GetTextBuffer(SourceLocation sourceLocation, out ITextBuffer textBuffer)
        {
            return VsIdeScope.GetTextBuffer(sourceLocation, out textBuffer);
        }

        public SyntaxTree GetSyntaxTree(ITextBuffer textBuffer)
        {
            return VsIdeScope.GetSyntaxTree(textBuffer);
        }

        public Task RunOnBackgroundThread(Func<Task> action, Action<Exception> onException, string threadName)
        {
            return VsIdeScope.RunOnBackgroundThread(action, onException, threadName);
        }

        public Task RunOnUiThread(Action action)
        {
            return VsIdeScope.RunOnUiThread(action);
        }

        public void OpenIfNotOpened(string path)
        {
            VsIdeScope.OpenIfNotOpened(path);
        }

        public IProjectScope[] GetProjectsWithFeatureFiles()
        {
            return VsIdeScope.GetProjectsWithFeatureFiles();
        }

        public IDisposable CreateUndoContext(string undoLabel)
        {
            return VsIdeScope.CreateUndoContext(undoLabel);
        }

        public IServiceProvider ServiceProvider => VsIdeScope.ServiceProvider;
        public DTE Dte => VsIdeScope.Dte;
        public IDeveroomOutputPaneServices DeveroomOutputPaneServices => VsIdeScope.DeveroomOutputPaneServices;
        public IDeveroomErrorListServices DeveroomErrorListServices => VsIdeScope.DeveroomErrorListServices;

        public IProjectScope GetProjectScope(Project project)
        {
            return VsIdeScope.GetProjectScope(project);
        }

        public void Dispose()
        {
            VsIdeScope.Dispose();
        }
        #endregion
    }
}
