using System;
using System.IO.Abstractions;
using SpecFlow.VisualStudio.Diagnostics;

namespace SpecFlow.VisualStudio.ProjectSystem.Configuration
{
    public class ConfigSource
    {
        public string FilePath { get; }
        public DateTimeOffset LastChangeTime { get; }

        public ConfigSource(string filePath, DateTimeOffset lastChangeTime)
        {
            FilePath = filePath;
            LastChangeTime = lastChangeTime;
        }

        public static ConfigSource TryGetConfigSource(string filePath, IFileSystem fileSystem, IDeveroomLogger logger)
        {
            if (!fileSystem.File.Exists(filePath))
                return null;
            try
            {
                return new ConfigSource(filePath, fileSystem.File.GetLastWriteTimeUtc(filePath));
            }
            catch (Exception ex)
            {
                logger.LogDebugException(ex);
                return null;
            }
        }

        protected bool Equals(ConfigSource other)
        {
            return Equals(FilePath, other.FilePath) && LastChangeTime.Equals(other.LastChangeTime);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((ConfigSource) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (FilePath.GetHashCode() * 397) ^ LastChangeTime.GetHashCode();
            }
        }
    }
}