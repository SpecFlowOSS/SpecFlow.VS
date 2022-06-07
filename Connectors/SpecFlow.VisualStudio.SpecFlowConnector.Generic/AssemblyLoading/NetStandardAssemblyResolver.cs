namespace SpecFlowConnector.AssemblyLoading;

public class NetStandardAssemblyResolver : DotNetResolver
{
    protected override bool CanHandleLibraryName(string libraryName) =>
        libraryName == "netstandard";

    protected override string RootDirectory(string programFiles) => Path.Combine(
        programFiles,
        "dotnet",
        "shared",
        "Microsoft.NETCore.App");

    protected override string SearchPattern(string[] versionParts) => "*";
}
