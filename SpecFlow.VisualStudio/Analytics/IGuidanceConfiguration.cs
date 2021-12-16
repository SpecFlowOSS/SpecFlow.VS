using System;
using System.Linq;

namespace SpecFlow.VisualStudio.Analytics;

public interface IGuidanceConfiguration
{
    GuidanceStep Installation { get; }

    GuidanceStep Upgrade { get; }

    IEnumerable<GuidanceStep> UsageSequence { get; }
}
