using Deveroom.VisualStudio.Discovery.TagExpressions;

namespace Deveroom.VisualStudio.Discovery
{
    public class Scope
    {
        public ITagExpression Tag { get; set; }
        public string FeatureTitle { get; set; }
        public string ScenarioTitle { get; set; }
    }
}
