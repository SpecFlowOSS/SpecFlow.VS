using System;
using System.IO.Abstractions;
using SpecFlow.VisualStudio.Diagnostics;

namespace SpecFlow.VisualStudio.ProjectSystem.Configuration
{
    public record ConfigSource(string FilePath, DateTimeOffset LastChangeTime)
    {
        public static ConfigSource Invalid = new (string.Empty, DateTimeOffset.MinValue);

        public static ConfigSource TryGetConfigSource(string filePath, IFileSystem fileSystem, IDeveroomLogger logger)
        {
            if (!fileSystem.File.Exists(filePath))
                return Invalid;
            try
            {
                return new ConfigSource(filePath, fileSystem.File.GetLastWriteTimeUtc(filePath));
            }
            catch (Exception ex)
            {
                logger.LogDebugException(ex);
                return Invalid;
            }
        }
    }
}