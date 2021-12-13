#nullable enable
using IServiceProvider = System.IServiceProvider;
using Project = EnvDTE.Project;

namespace SpecFlow.VisualStudio.ProjectSystem
{
    public interface IVsIdeScope : IIdeScope, IDisposable
    {
        IServiceProvider ServiceProvider { get; }
        DTE Dte { get; }
        IProjectScope GetProjectScope(Project project);
    }
}