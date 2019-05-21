using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Deveroom.VisualStudio.Common;

namespace Deveroom.VisualStudio.ProjectSystem
{
    public static class ProjectSystemExtensions
    {
        public static string GetExtensionFolder(this IIdeScope ideScope)
        {
            var extensionFolder = Path.GetDirectoryName(typeof(ProjectSystemExtensions).Assembly.GetLocalCodeBase());
            Debug.Assert(extensionFolder != null);
            return extensionFolder ?? ideScope.FileSystem.Directory.GetCurrentDirectory();
        }
    }
}
