using SpecFlowConnector.AssemblyLoading;

namespace SpecFlow.VisualStudio.SpecFlowConnector.Tests;

public class AssemblyLoadingTests
{
    private readonly List<Assembly> _assemblies =
        AssembliesInDir("..\\..\\..\\..\\SpecFlow.VisualStudio.Specs\\bin\\Debug", "Spec*.dll")
            .Union(AssembliesInDir(".", "Spec*.dll"))
            .Union(new[] {typeof(AssemblyLoadingTests).Assembly})
            .ToList();

    private readonly ITestOutputHelper _testOutputHelper;

    public AssemblyLoadingTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    private static IEnumerable<Assembly> AssembliesInDir(string path, string searchPattern) =>
        true//Directory.Exists(path)
            ? Directory
                .EnumerateFiles(path, searchPattern, SearchOption.AllDirectories)
                .Select(s => Path.GetFullPath(s))
                .Select(Assembly.LoadFrom)
            : Array.Empty<Assembly>();

    [Fact]
    public void Load_Assemblies()
    {
        //act
        var log = new TestOutputHelperLogger(_testOutputHelper);
        var loadContexts = _assemblies.Select(assembly => new TestAssemblyLoadContext(
                assembly.Location,
                (assemblyLoadContext, path) => assemblyLoadContext.LoadFromAssemblyPath(path),
                log))
            .ToList();

        var a = loadContexts[0].LoadFromAssemblyName(new AssemblyName("Microsoft.AspNetCore.Antiforgery")
            {Version = new Version(6, 0)});

        var loadedAssemblies = loadContexts
            .SelectMany(lc => _assemblies.Select(a => lc.LoadFromAssemblyName(a.GetName())))
            .ToList();


        //assert
        loadedAssemblies.Should().HaveCountGreaterThan(0);
        loadContexts.Select(lc => lc.TestAssembly.Location).Should()
            .BeEquivalentTo(_assemblies.Select(a => a.Location), "same dlls are loaded");
        loadContexts.Select(lc => lc.TestAssembly).Should()
            .NotBeEquivalentTo(_assemblies, "assemblies re loaded into different context");
    }

    [Fact]
    public void Loads_From_AspNetCoreAssembly()
    {
        //arrange
        var log = new TestOutputHelperLogger(_testOutputHelper);

        var loadContext = new TestAssemblyLoadContext(
            GetType().Assembly.Location,
            (assemblyLoadContext, path) => assemblyLoadContext.LoadFromAssemblyPath(path),
            log);

        //act
        var loadedAssembly = loadContext.LoadFromAssemblyName(new AssemblyName("Microsoft.AspNetCore.Antiforgery")
            {Version = new Version(6, 0)});

        //assert
        loadedAssembly.GetName().Name.Should().Be("Microsoft.AspNetCore.Antiforgery");
    }
}
