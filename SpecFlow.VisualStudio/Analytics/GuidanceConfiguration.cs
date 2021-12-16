#nullable disable
namespace SpecFlow.VisualStudio.Analytics;

[Export(typeof(IGuidanceConfiguration))]
public class GuidanceConfiguration : IGuidanceConfiguration
{
    public GuidanceConfiguration()
    {
        Installation = new GuidanceStep(GuidanceNotification.AfterInstall, null,
            @"https://specflow.org/welcome-to-specflow/");

        Upgrade = new GuidanceStep(GuidanceNotification.Upgrade, null, null);

        UsageSequence = new[]
        {
            new GuidanceStep(GuidanceNotification.TwoDayUsage, 2, null),
            new GuidanceStep(GuidanceNotification.FiveDayUsage, 5, null),
            new GuidanceStep(GuidanceNotification.TenDayUsage, 10, null),
            new GuidanceStep(GuidanceNotification.TwentyDayUsage, 20, null),
            new GuidanceStep(GuidanceNotification.HundredDayUsage, 100, null),
            new GuidanceStep(GuidanceNotification.TwoHundredDayUsage, 200, null)
        };
    }

    public GuidanceStep Installation { get; }

    public GuidanceStep Upgrade { get; }

    public IEnumerable<GuidanceStep> UsageSequence { get; }
}
