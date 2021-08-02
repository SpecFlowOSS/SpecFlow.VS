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
                _dispatcher.BeginInvoke(action, DispatcherPriority.ContextIdle);
            }
        }
    }
}