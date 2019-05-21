using System;
using System.Linq;
using Gherkin.Ast;

namespace Deveroom.VisualStudio.Discovery
{
    public class MatchResult
    {
        private static readonly string[] EmptyErrors = new string[0];

        public MatchResultItem[] Items { get; }

        public string[] Errors { get; }
        public bool HasErrors => Errors.Any();

        public bool HasUndefined =>
            Items.Any(m => m.Type == MatchResultType.Undefined);

        public bool HasDefined =>
            Items.Any(m => m.Type == MatchResultType.Defined);

        public bool HasAmbiguous =>
            Items.Any(m => m.Type == MatchResultType.Ambiguous);

        public bool HasMultipleMatches =>
            Items.Length > 1;

        public bool HasSingleMatch =>
            Items.Length == 1;

        private MatchResult(MatchResultItem[] items, string[] errors)
        {
            Items = items ?? throw new ArgumentNullException(nameof(items));
            if (items.Length == 0)
                throw new ArgumentException("Match result should contain at least one item", nameof(items));
            Errors = errors ?? EmptyErrors;
        }

        public override string ToString()
        {
            return string.Join(Environment.NewLine, Items.Select(sd => sd.ToString()));
        }

        public string GetErrorMessage()
        {
            if (!HasErrors)
                return null;
            return string.Join(Environment.NewLine, Errors);
        }

        public static MatchResult CreateMultiMatch(MatchResultItem[] items)
        {
            if (items == null) throw new ArgumentNullException(nameof(items));

            var errors = items.SelectMany(m => m.Errors);
            if (items.Any(m => m.Type == MatchResultType.Ambiguous))
            {
                var ambiguousMatches = items.Where(m => m.Type == MatchResultType.Ambiguous);
                var ambiguousErrorMessage = $"Ambiguous steps: {Environment.NewLine}{string.Join(Environment.NewLine, ambiguousMatches.Select(sd => sd.MatchedStepDefinition.ToString()))}";
                errors = errors.Concat(new[] { ambiguousErrorMessage });
            }

            return new MatchResult(items, errors.ToArray());
        }
    }
}