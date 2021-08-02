using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SpecFlow.VisualStudio.ProjectSystem;
using TechTalk.SpecFlow;

namespace Deveroom.VisualStudio.Specs.Support
{
    [Binding]
    public class Converters
    {
        [StepArgumentTransformation(@"the latest version")]
        [StepArgumentTransformation(@"v2\.\*")]
        public NuGetVersion LatestVersionConverter()
        {
            return DomainDefaults.LatestSpecFlowV2Version;
        }

        [StepArgumentTransformation(@"the latest V3 version")]
        [StepArgumentTransformation(@"v3\.\*")]
        [StepArgumentTransformation(@"v3\.1\.\*")]
        public NuGetVersion LatestV3VersionConverter()
        {
            return DomainDefaults.LatestSpecFlowV3Version;
        }

        [StepArgumentTransformation(@"v(\d[\d\.\-\w]+)")]
        public NuGetVersion VersionConverter(string versionString)
        {
            return new NuGetVersion(versionString);
        }

        [StepArgumentTransformation]
        public string[] CommaSeparatedList(string list)
        {
            return list.Split(new[] {' ', ','}, StringSplitOptions.RemoveEmptyEntries);
        }

        [StepArgumentTransformation]
        public int[] CommaSeparatedIntList(string list)
        {
            return CommaSeparatedList(list).Select(int.Parse).ToArray();
        }
    }
}
