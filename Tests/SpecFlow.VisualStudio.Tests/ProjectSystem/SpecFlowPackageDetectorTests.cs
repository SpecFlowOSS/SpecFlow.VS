using System;
using System.Linq;

namespace SpecFlow.VisualStudio.Tests.ProjectSystem;

public class SpecFlowPackageDetectorTests
{
    private const string SpecFlowPackagePath = @"C:\Users\me\.nuget\packages\specflow\2.4.1";
    private const string SpecFlow240PackagePath = @"C:\Users\me\.nuget\packages\specflow\2.4.0";
    private const string SpecFlowMsTestPackagePath = @"C:\Users\me\.nuget\packages\specflow.mstest\2.4.1";
    private const string SpecSyncPackagePath = @"C:\Users\me\.nuget\packages\specsync.azuredevops.specflow.2-4\2.0.0";

    private const string SpecSyncPackagePathSolutionPackages =
        @"C:\MyProject\packages\SpecSync.AzureDevOps.SpecFlow.2-4.2.0.0";

    private const string SpecFlow240PackagePathSolutionPackages = @"C:\MyProject\packages\SpecFlow.2.4.0";
    private readonly MockFileSystem _mockFileSystem = new();

    public SpecFlowPackageDetectorTests()
    {
        _mockFileSystem.AddDirectory(SpecFlowPackagePath);
        _mockFileSystem.AddDirectory(SpecFlow240PackagePath);
        _mockFileSystem.AddDirectory(SpecFlowMsTestPackagePath);
        _mockFileSystem.AddDirectory(SpecSyncPackagePath);
    }

    private static NuGetPackageReference CreateSpecFlowPackageRef() =>
        new("SpecFlow", new NuGetVersion("2.4.1", "2.4.1"), SpecFlowPackagePath);

    private static NuGetPackageReference CreateSpecFlowMsTestPackageRef() =>
        new("SpecFlow.MsTest", new NuGetVersion("2.4.1", "2.4.1"), SpecFlowMsTestPackagePath);

    private NuGetPackageReference CreateSpecSyncPackageRef(string path = SpecSyncPackagePath) =>
        new("SpecSync.AzureDevOps.SpecFlow.2-4", new NuGetVersion("2.0.0", "2.0.0"), path);

    private SpecFlowPackageDetector CreateSut() => new(_mockFileSystem);

    [Fact]
    public void GetSpecFlowPackage_returns_null_for_empty_package_list()
    {
        var sut = CreateSut();

        var result = sut.GetSpecFlowPackage(new NuGetPackageReference[0]);

        result.Should().BeNull();
    }

    [Fact]
    public void GetSpecFlowPackage_finds_SpecFlow_package_when_listed()
    {
        var sut = CreateSut();

        var result = sut.GetSpecFlowPackage(new[]
        {
            CreateSpecFlowPackageRef()
        });

        result.Should().NotBeNull();
        result.PackageName.Should().Be("SpecFlow");
    }

    [Fact]
    public void
        GetSpecFlowPackage_finds_SpecFlow_package_when_listed_even_if_other_specflow_related_packages_are_listed_before()
    {
        var sut = CreateSut();

        var result = sut.GetSpecFlowPackage(new[]
        {
            CreateSpecSyncPackageRef(),
            CreateSpecFlowMsTestPackageRef(),
            CreateSpecFlowPackageRef()
        });

        result.Should().NotBeNull();
        result.PackageName.Should().Be("SpecFlow");
    }

    [Fact]
    public void GetSpecFlowPackage_finds_SpecFlow_package_when_only_other_SpecFlow_packages_are_listed()
    {
        var sut = CreateSut();

        var result = sut.GetSpecFlowPackage(new[]
        {
            CreateSpecSyncPackageRef(),
            CreateSpecFlowMsTestPackageRef()
        });

        result.Should().NotBeNull();
        result.PackageName.Should().Be("SpecFlow");
        result.Version.Should().Be(new NuGetVersion("2.4.1", "2.4.1"));
    }

    [Fact]
    public void GetSpecFlowPackage_finds_SpecFlow_package_when_only_SpecFlow_extension_packages_are_listed()
    {
        var sut = CreateSut();

        var result = sut.GetSpecFlowPackage(new[]
        {
            CreateSpecSyncPackageRef()
        });

        result.Should().NotBeNull();
        result.PackageName.Should().Be("SpecFlow");
        result.Version.Should().Be(new NuGetVersion("2.4.0", "2.4.0"));
    }

    [Fact]
    public void
        GetSpecFlowPackage_finds_SpecFlow_package_within_solution_when_only_SpecFlow_extension_packages_are_listed()
    {
        _mockFileSystem.AddDirectory(SpecFlow240PackagePathSolutionPackages);
        _mockFileSystem.AddDirectory(SpecSyncPackagePathSolutionPackages);
        var sut = CreateSut();

        var result = sut.GetSpecFlowPackage(new[]
        {
            CreateSpecSyncPackageRef(SpecSyncPackagePathSolutionPackages)
        });

        result.Should().NotBeNull();
        result.PackageName.Should().Be("SpecFlow");
        result.Version.Should().Be(new NuGetVersion("2.4.0", "2.4.0"));
        result.InstallPath.Should().BeEquivalentTo(SpecFlow240PackagePathSolutionPackages);
    }

    [Fact]
    public void GetSpecFlowPackage_returns_null_when_extension_package_has_no_path()
    {
        var sut = CreateSut();

        var result = sut.GetSpecFlowPackage(new[]
        {
            CreateSpecSyncPackageRef(null)
        });

        result.Should().BeNull();
    }
}
