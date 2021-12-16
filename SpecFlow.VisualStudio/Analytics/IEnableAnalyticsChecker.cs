using System;
using System.Linq;

namespace SpecFlow.VisualStudio.Analytics;

public interface IEnableAnalyticsChecker
{
    bool IsEnabled();
}

[Export(typeof(IEnableAnalyticsChecker))]
public class EnableAnalyticsChecker : IEnableAnalyticsChecker
{
    public const string SpecFlowTelemetryEnvironmentVariable = "SPECFLOW_TELEMETRY_ENABLED";

    public bool IsEnabled()
    {
        var specFlowTelemetry = Environment.GetEnvironmentVariable(SpecFlowTelemetryEnvironmentVariable);
        return specFlowTelemetry == null || specFlowTelemetry.Equals("1");
    }
}
