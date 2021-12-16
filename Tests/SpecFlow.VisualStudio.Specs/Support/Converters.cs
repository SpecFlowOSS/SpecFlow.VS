using System;
using System.Linq;

namespace SpecFlow.VisualStudio.Specs.Support;

[Binding]
public class Converters
{
    [StepArgumentTransformation(@"the latest version")]
    [StepArgumentTransformation(@"v2\.\*")]
    public NuGetVersion LatestVersionConverter() => DomainDefaults.LatestSpecFlowV2Version;

    [StepArgumentTransformation(@"the latest V3 version")]
    [StepArgumentTransformation(@"v3\.\*")]
    [StepArgumentTransformation(@"v3\.1\.\*")]
    public NuGetVersion LatestV3VersionConverter() => DomainDefaults.LatestSpecFlowV3Version;

    [StepArgumentTransformation(@"v(\d[\d\.\-\w]+)")]
    public NuGetVersion VersionConverter(string versionString) => new(versionString, versionString);

    [StepArgumentTransformation]
    public string[] CommaSeparatedList(string list)
    {
        return list.Split(new[] {' ', ','}, StringSplitOptions.RemoveEmptyEntries);
    }

    [StepArgumentTransformation]
    public int[] CommaSeparatedIntList(string list) => CommaSeparatedList(list).Select(int.Parse).ToArray();
}
