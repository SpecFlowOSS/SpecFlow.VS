using SpecFlow.VisualStudio.Discovery.TagExpressions;

namespace SpecFlow.VisualStudio.Discovery
{
    public class Scope
    {
        public ITagExpression Tag { get; set; }
        public string FeatureTitle { get; set; }
        public string ScenarioTitle { get; set; }
    }
}
