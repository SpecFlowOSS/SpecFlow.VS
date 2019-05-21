using System;
using Deveroom.VisualStudio.Diagnostics;
using EnvDTE;

namespace Deveroom.VisualStudio.ProjectSystem
{
    public interface IVsIdeScope : IIdeScope, IDisposable
    {
        IServiceProvider ServiceProvider { get; }
        DTE Dte { get; }
        IProjectScope GetProjectScope(Project project);
    }
}