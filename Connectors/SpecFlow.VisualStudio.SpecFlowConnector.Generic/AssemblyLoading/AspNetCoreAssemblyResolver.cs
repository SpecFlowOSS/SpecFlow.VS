﻿namespace SpecFlowConnector.AssemblyLoading;

public class AspNetCoreAssemblyResolver : DotNetResolver
{
    protected override bool CanHandleLibraryName(string libraryName) =>
        libraryName.StartsWith("Microsoft.AspNetCore") || libraryName.StartsWith("Microsoft.Extensions");

    protected override string RootDirectory(string programFiles) => Path.Combine(
        programFiles,
        "dotnet",
        "shared",
        "Microsoft.AspNetCore.App");

}
