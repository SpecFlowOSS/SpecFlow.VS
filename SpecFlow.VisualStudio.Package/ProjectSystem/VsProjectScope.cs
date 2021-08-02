using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using SpecFlow.VisualStudio.Common;
using SpecFlow.VisualStudio.Diagonostics;
using SpecFlow.VisualStudio.Monitoring;
using EnvDTE;
using Microsoft.VisualStudio.Utilities;
using NuGet.VisualStudio;

namespace SpecFlow.VisualStudio.ProjectSystem
{
    public class VsProjectScope : IProjectScope
    {
        private readonly Project _project;
        private readonly IVsPackageInstallerServices _vsPackageInstallerServices;
        public PropertyCollection Properties { get; } = new PropertyCollection();
        public string ProjectFolder { get; }
        public string OutputAssemblyPath => VsUtils.GetOutputAssemblyPath(_project);
        public string TargetFrameworkMoniker => VsUtils.GetTargetFrameworkMoniker(_project);
        public string PlatformTargetName => VsUtils.GetPlatformTargetName(_project) ?? VsUtils.GetPlatformName(_project);
        public string ProjectName { get; }
        public string DefaultNamespace => GetDefaultNamespace();

        public VsProjectScope(string id, Project project, IIdeScope ideScope, IVsPackageInstallerServices vsPackageInstallerServices)
        {
            _project = project;
            _vsPackageInstallerServices = vsPackageInstallerServices;
            IdeScope = ideScope;
            ProjectFolder = VsUtils.GetProjectFolder(project);
            ProjectName = project.Name;
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
            if (_vsPackageInstallerServices == null)
                return new NuGetPackageReference[0];

            try
            {
                return _vsPackageInstallerServices.GetInstalledPackages(_project)
                    .Select(pmd => new NuGetPackageReference(pmd.Id, new NuGetVersion(pmd.VersionString), pmd.InstallPath))
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
