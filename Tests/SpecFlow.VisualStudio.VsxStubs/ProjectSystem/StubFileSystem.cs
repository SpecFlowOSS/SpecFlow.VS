using System.IO.Abstractions;

namespace SpecFlow.VisualStudio.VsxStubs.ProjectSystem
{
    public class StubFileSystem : IFileSystem
    {
        public IFile File { get; } = new StubFile();
        public IDirectory Directory { get; }
        public IFileInfoFactory FileInfo { get; }
        public IFileStreamFactory FileStream { get; }
        public IPath Path { get; }
        public IDirectoryInfoFactory DirectoryInfo { get; }
        public IDriveInfoFactory DriveInfo { get; }
        public IFileSystemWatcherFactory FileSystemWatcher { get; }
    }
}