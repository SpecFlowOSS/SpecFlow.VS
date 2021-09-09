using System;
using System.Windows.Threading;
using Microsoft.VisualStudio.Shell;

namespace SpecFlow.VisualStudio.ProjectSystem
{
    public abstract class VsServiceBase
    {
        protected readonly IVsIdeScope _vsIdeScope;
        private readonly Dispatcher _dispatcher;

        protected VsServiceBase(IVsIdeScope vsIdeScope)
        {
            _vsIdeScope = vsIdeScope;
            _dispatcher = Dispatcher.CurrentDispatcher;
        }

        protected void RunOnUiThread(Action action)
        {
            if (ThreadHelper.CheckAccess())
            {
                action();
            }
            else
            {
#pragma warning disable VSTHRD001 // Avoid legacy thread switching APIs
                _dispatcher.BeginInvoke(action, DispatcherPriority.ContextIdle);
#pragma warning restore VSTHRD001 // Avoid legacy thread switching APIs
            }
        }
    }
}