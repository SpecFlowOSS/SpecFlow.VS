using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.Utilities;

namespace Deveroom.VisualStudio.ProjectSystem
{
    public interface IProjectScope : IPropertyOwner, IDisposable
    {
        IIdeScope IdeScope { get; }
        string ProjectName { get; }
        string ProjectFolder { get; }

        IEnumerable<NuGetPackageReference> PackageReferences { get; }
        string OutputAssemblyPath { get; }
        string TargetFrameworkMoniker { get; }
        string DefaultNamespace { get; }

        void AddFile(string targetFilePath, string template);
        int? GetFeatureFileCount();
        string[] GetProjectFiles(string extension);
    }
}
