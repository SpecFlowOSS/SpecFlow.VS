// ReSharper disable once CheckNamespace

namespace System.Reflection;

internal static class AssemblyExtensions
{
    public static Either<Exception, FileDetails> GetLocalCodeBase(this Assembly assembly)
    {
#if NETFRAMEWORK
        return GetLocalCodeBase(assembly.CodeBase, Path.DirectorySeparatorChar);
#else
        return GetLocalCodeBase(assembly.Location, Path.DirectorySeparatorChar);
#endif
    }

    private static Either<Exception, FileDetails> GetLocalCodeBase(string codeBase, char directorySeparator)
    {
        if (directorySeparator is not ('/' or '\\'))
            return new ArgumentException(
                $"Unknown directory separator '{directorySeparator}'; must be one of '/' or '\\'.",
                nameof(directorySeparator));

        var uri = new Uri(codeBase);

        if (directorySeparator == '/' && uri.IsUnc)
            return new ArgumentException(
                $"UNC-style codebase '{codeBase}' is not supported on POSIX-style file systems.", nameof(codeBase));

        return FileDetails.FromPath(uri.LocalPath);
    }
}
