using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SpecFlow.VisualStudio.Common;
using SpecFlow.VisualStudio.Diagnostics;
using SpecFlow.VisualStudio.Monitoring;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Utilities;
using NuGet.VisualStudio.Contracts;

namespace SpecFlow.VisualStudio.ProjectSystem
{
    public class VsProjectScope : IProjectScope
    {
        private readonly Project _project;
        public PropertyCollection Properties { get; } = new PropertyCollection();
        public string ProjectFolder { get; }
        public string OutputAssemblyPath => VsUtils.GetOutputAssemblyPath(_project);
        public string TargetFrameworkMoniker => VsUtils.GetTargetFrameworkMoniker(_project);
        public string TargetFrameworkMonikers => VsUtils.GetTargetFrameworkMonikers(_project);
        public string PlatformTargetName => VsUtils.GetPlatformTargetName(_project) ?? VsUtils.GetPlatformName(_project);
        public string ProjectName { get; }
        public string ProjectFullName { get; }
        public string DefaultNamespace => GetDefaultNamespace();

        public VsProjectScope(string id, Project project, IIdeScope ideScope)
        {
            _project = project;
            IdeScope = ideScope;
            ProjectFolder = VsUtils.GetProjectFolder(project);
            ProjectName = project.Name;
            ProjectFullName = project.FullName;
            Debug.Assert(ProjectFolder != null, "VsxHelper.IsSolutionProject ensures a not-null ProjectFolder");
        }

        public IIdeScope IdeScope { get; }
        public IEnumerable<NuGetPackageReference> PackageReferences => GetPackageReferences();
        private IDeveroomLogger Logger => IdeScope.Logger;
        private IMonitoringService MonitoringService => IdeScope.MonitoringService;

        private string GetDefaultNamespace()
        {
            try
            {
                return _project.Properties.Item("DefaultNamespace")?.Value as string;
            }
            catch (Exception e)
            {
                Logger.LogException(MonitoringService, e);
                return null;
            }
        }

        private NuGetPackageReference[] GetPackageReferences()
        {
            try
            {
                return VsUtils.GetInstalledNuGetPackages((IdeScope as VsIdeScope).ServiceProvider, _project.FullName)
                    .Select(pmd => new NuGetPackageReference(pmd.Id, new NuGetVersion(pmd.Version, pmd.RequestedRange), pmd.InstallPath))
                    .ToArray();
            }
            catch (Exception e)
            {
                if (IdeScope.IsSolutionLoaded)
                    Logger.LogVerboseException(MonitoringService, e);
                else
                    Logger.LogVerbose("Loading package references failed, solution is not loaded fully yet.");
                return null;
            }
        }

        public void AddFile(string targetFilePath, string template)
        {
            //TODO: handle template parameters
            IdeScope.FileSystem.File.WriteAllText(targetFilePath, template); 
            _project.ProjectItems.AddFromFile(targetFilePath);
        }

        public int? GetFeatureFileCount()
        {
            try
            {
                return VsUtils.GetPhysicalFileProjectItems(_project)
                    .Count(pi => FileSystemHelper.IsOfType(VsUtils.GetFilePath(pi), ".feature"));
            }
            catch (Exception e)
            {
                Logger.LogVerboseException(MonitoringService, e);
                return null;
            }
        }

        public string[] GetProjectFiles(string extension)
        {
            try
            {
                return VsUtils.GetPhysicalFileProjectItems(_project)
                    .Select(VsUtils.GetFilePath)
                    .Where(fp => FileSystemHelper.IsOfType(fp, ".feature"))
                    .ToArray();
            }
            catch (Exception e)
            {
                Logger.LogVerboseException(MonitoringService, e);
                return new string[0];
            }
        }

        public override string ToString()
        {
            return ProjectName;
        }

        public void Dispose()
        {
            foreach (var disposableProperty in Properties.PropertyList.Select(p => p.Value).OfType<IDisposable>())
            {
                disposableProperty.Dispose();
            }
        }
    }
}
