namespace SpecFlowConnector.AssemblyLoading;

public abstract class DotNetResolver : ICompilationAssemblyResolver
{
    public bool TryResolveAssemblyPaths(CompilationLibrary library, List<string> assemblies) => library
        .Validate(_ => CanHandleLibraryName(library.Name))
        .Validate(_ => !string.IsNullOrEmpty(library.Version))
        .Map(_ => Environment.GetEnvironmentVariable("ProgramFiles")
            .AsOption()
            .Validate(programFiles => !string.IsNullOrEmpty(programFiles))
            .Map(RootDirectory)
            .Validate(Directory.Exists)
            .Map(rootDirectory => library.Version.Split('.')
                .Validate(versionParts => versionParts.Length >= 2)
                .Map(SearchPattern)
                .Map(searchPattern => FileAsset(library.Assemblies)
                    .Map(Path.GetFileName)
                    .MapOptional(assemblyFileName => assemblyFileName.AsOption())
                    .Map(assemblyFileName => DotnetDirectory(rootDirectory, searchPattern, assemblyFileName)
                        .Tie(dotNetDirectory =>
                            assemblies.Add(Path.Combine(dotNetDirectory, assemblyFileName)))
                    )
                )
            ))
        .Map(_ => true)
        .Reduce(false);

    protected abstract bool CanHandleLibraryName(string libraryName);

    protected abstract string RootDirectory(string programFiles);

    protected virtual string SearchPattern(string[] versionParts) => $"{versionParts[0]}.{versionParts[1]}.*";

    protected Option<string> FileAsset(IEnumerable<string>? assemblies) => assemblies?.FirstOrDefault();

    protected Option<string> DotnetDirectory(string rootFolder, string searchPattern, string assemblyFileName) =>
        Directory.GetDirectories(rootFolder, searchPattern, SearchOption.TopDirectoryOnly)
            .Where(d => File.Exists(Path.Combine(d, assemblyFileName)))
            .OrderByDescending(d => d)
            .FirstOrDefault();
}
