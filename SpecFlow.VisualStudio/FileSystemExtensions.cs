using System;
using System.IO.Abstractions;

namespace SpecFlow.VisualStudio
{
    public static class FileSystemExtensions
    {
        public static string GetFilePathIfExists(this IFileSystem fileSystem, string filePath)
        {
            if (fileSystem.File.Exists(filePath))
                return filePath;
            return null;
        }
    }
}
