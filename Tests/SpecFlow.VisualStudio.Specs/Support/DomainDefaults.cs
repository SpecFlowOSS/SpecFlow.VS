using System;
using System.Linq;
using SpecFlow.VisualStudio.ProjectSystem;

namespace SpecFlow.VisualStudio.Specs.Support
{
    public static class DomainDefaults
    {
        //TODO: calculate latest versions automatically
        public static NuGetVersion LatestSpecFlowV2Version = new NuGetVersion("2.4.1");
        public static NuGetVersion LatestSpecFlowV3Version = new NuGetVersion("3.6.23");
    }
}
