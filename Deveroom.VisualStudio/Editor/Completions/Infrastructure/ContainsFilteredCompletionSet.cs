using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;

namespace Deveroom.VisualStudio.Editor.Completions.Infrastructure
{
    public class ContainsFilteredCompletionSet : ExtendableCompletionSet
    {
        public ContainsFilteredCompletionSet(string moniker, string displayName, ITrackingSpan applicableTo, IEnumerable<Completion> completions, IEnumerable<Completion> completionBuilders) : base(moniker, displayName, applicableTo, completions, completionBuilders)
        {
        }

        protected override bool DoesTextMatch(string text, string filterText, bool caseSensitive)
        {
            var comparison = caseSensitive ? StringComparison.CurrentCulture : StringComparison.CurrentCultureIgnoreCase;
            return text.IndexOf(filterText, comparison) >= 0;
        }
    }
}
