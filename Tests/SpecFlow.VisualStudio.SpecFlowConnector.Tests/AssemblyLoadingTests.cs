using SpecFlowConnector.AssemblyLoading;

namespace SpecFlow.VisualStudio.SpecFlowConnector.Tests;

public class AssemblyLoadingTests
{
    private ITestOutputHelper _testOutputHelper;

    private readonly List<Assembly> _assemblies =
        AssembliesInDir("..\\..\\..\\..\\SpecFlow.VisualStudio.Specs\\bin\\Debug\\net472", "Spec*.dll")
            .Union(AssembliesInDir(".", "Spec*.dll"))
            .Union(new[] {typeof(AssemblyLoadingTests).Assembly})
            .ToList();

    public AssemblyLoadingTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    private static IEnumerable<Assembly> AssembliesInDir(string path, string searchPattern) =>
        Directory
            .EnumerateFiles(path, searchPattern)
            .Select(Path.GetFullPath)
            .Select(Assembly.LoadFrom);

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
}
