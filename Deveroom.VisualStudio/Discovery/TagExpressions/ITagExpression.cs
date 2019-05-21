using System.Collections.Generic;

namespace Deveroom.VisualStudio.Discovery.TagExpressions
{
    public interface ITagExpression
    {
        bool Evaluate(IEnumerable<string> variables);
    }
}
