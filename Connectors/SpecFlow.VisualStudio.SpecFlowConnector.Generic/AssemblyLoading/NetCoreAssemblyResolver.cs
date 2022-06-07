namespace SpecFlowConnector.AssemblyLoading;

public class NetCoreAssemblyResolver : DotNetResolver
{
    protected override bool CanHandleLibraryName(string libraryName) =>
        libraryName.StartsWith("System");

    protected override string RootDirectory(string programFiles) => Path.Combine(
        programFiles,
        "dotnet",
        "shared",
        "Microsoft.NETCore.App");
}