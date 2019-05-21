using System.Collections.Generic;
using System.Linq;
using Gherkin.Ast;

namespace Deveroom.VisualStudio.Discovery.TagExpressions
{
    public static class TagExpressionExtensions
    {
        public static bool EvaluateWithDefault(this ITagExpression tagExpression, IEnumerable<string> tags, bool defaultValue)
        {
            return tagExpression?.Evaluate(tags) ?? defaultValue;
        }

        public static bool EvaluateWithDefault(this ITagExpression tagExpression, IEnumerable<Tag> tags, bool defaultValue)
        {
            return tagExpression?.Evaluate(tags.Select(t => t.Name)) ?? defaultValue;
        }
    }
}
