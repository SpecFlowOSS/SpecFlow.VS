using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Deveroom.VisualStudio.ProjectSystem;
using TechTalk.SpecFlow;

namespace Deveroom.VisualStudio.Specs.Support
{
    [Binding]
    public class Converters
    {
        [StepArgumentTransformation(@"the latest version")]
        public NuGetVersion LatestVersionConverter()
        {
            return DomainDefaults.LatestSpecFlowVersion;
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
