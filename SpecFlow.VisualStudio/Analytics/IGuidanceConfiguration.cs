using System;
using System.Collections.Generic;
using System.Linq;

namespace SpecFlow.VisualStudio.Analytics
{
    public interface IGuidanceConfiguration
    {
        GuidanceStep Installation { get; }

        GuidanceStep Upgrade { get; }

        IEnumerable<GuidanceStep> UsageSequence { get; }
    }
}
