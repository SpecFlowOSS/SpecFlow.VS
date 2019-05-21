using System;
using System.Collections.Generic;
using System.Linq;
using Deveroom.VisualStudio.Editor.Services.Parser;
using Gherkin.Ast;

namespace Deveroom.VisualStudio.Discovery
{
    internal static class GherkinDocumentContextCalculator
    {
        private static T[] EnsureArray<T>(IEnumerable<T> enumerable)
        {
            if (enumerable == null)
                return null;
            if (enumerable is T[] array)
                return array;
            return enumerable.ToArray();
        }

        class TagMatchingContext : IGherkinDocumentContext
        {
            public IGherkinDocumentContext Parent { get; }
            public object Node { get; }

            public TagMatchingContext(IGherkinDocumentContext parent, object node)
            {
                Parent = parent;
                Node = node;
            }
        }

        class TagMatchingContextComparer : IEqualityComparer<IGherkinDocumentContext>
        {
            public static readonly TagMatchingContextComparer Instance = new TagMatchingContextComparer();

            public bool Equals(IGherkinDocumentContext x, IGherkinDocumentContext y)
            {
                if (x == null && y == null)
                    return true;
                if (x == null || y == null)
                    return false;

                return x.GetTagNames().SequenceEqual(y.GetTagNames());
            }

            public int GetHashCode(IGherkinDocumentContext obj)
            {
                var firstTag = obj.GetTagNames().FirstOrDefault();
                return firstTag?.GetHashCode() ?? 547;
            }
        }

        public static IEnumerable<KeyValuePair<string, IGherkinDocumentContext>> GetBackgroundStepsWithContexts(Step step, IGherkinDocumentContext context)
        {
            return GetBackgroundContexts(context).Distinct(TagMatchingContextComparer.Instance)
                .Select(ctx => new KeyValuePair<string, IGherkinDocumentContext>(step.Text, ctx));
        }

        private static IEnumerable<IGherkinDocumentContext> GetBackgroundContexts(IGherkinDocumentContext context)
        {
            var featureContext = context.GetParentOf<Feature>();
            if (!(featureContext?.Node is Feature feature))
                yield break;

            if (!feature.Children.OfType<IHasTags>().Any()) // if there are no scenarios yet, we use the feature context for matching
                yield return featureContext;

            foreach (var scenarioDefinition in feature.Children.OfType<IHasTags>())
            {
                var subContext = new TagMatchingContext(featureContext, scenarioDefinition);
                yield return subContext;
                if (scenarioDefinition is ScenarioOutline scenarioOutline) // create scopes for tagged example sets
                {
                    foreach (var exampleSet in scenarioOutline.Examples.Where(es => es.Tags?.Any() ?? false))
                    {
                        var exampleSetContext = new TagMatchingContext(subContext, exampleSet);
                        yield return exampleSetContext;
                    }
                }
            }
        }

        public static IEnumerable<KeyValuePair<string, IGherkinDocumentContext>> GetScenarioOutlineStepsWithContexts(Step step, IGherkinDocumentContext context)
        {
            var scenarioOutline = (ScenarioOutline)context.Node;

            var subContext = context;
            bool hasExamples = false;
            foreach (var scenarioOutlineExamples in scenarioOutline.Examples)
            {
                var exampleTags = EnsureArray(scenarioOutlineExamples.Tags);
                if (exampleTags != null && exampleTags.Any())
                {
                    subContext = new TagMatchingContext(context, scenarioOutlineExamples);
                }

                if (scenarioOutlineExamples.TableHeader != null && scenarioOutlineExamples.TableBody != null)
                {
                    var header = scenarioOutlineExamples.TableHeader.Cells.Select(c => c.Value).ToArray();
                    foreach (var exampleRow in scenarioOutlineExamples.TableBody)
                    {
                        hasExamples = true;
                        var stepText = MatchedScenarioOutlinePlaceholder.ReplaceScenarioOutlinePlaceholders(step,
                            match =>
                            {
                                int headerIndex = Array.IndexOf(header, match.Name);
                                if (headerIndex < 0)
                                    return match.Value;
                                return exampleRow.Cells.ElementAtOrDefault(headerIndex)?.Value ?? match.Value;
                            });

                        yield return new KeyValuePair<string, IGherkinDocumentContext>(stepText, subContext);
                    }
                }
            }

            if (!hasExamples)
            {
                // for empty SO, we create a context with the original step text (incl. placeholders)
                yield return new KeyValuePair<string, IGherkinDocumentContext>(step.Text, context);
            }
        }
    }
}
