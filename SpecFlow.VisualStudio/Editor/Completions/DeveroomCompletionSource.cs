using System;
using System.Collections.Generic;
using System.Linq;
using SpecFlow.VisualStudio.Editor.Completions.Infrastructure;
using SpecFlow.VisualStudio.Editor.Services;
using SpecFlow.VisualStudio.Editor.Services.Parser;
using SpecFlow.VisualStudio.ProjectSystem;
using SpecFlow.VisualStudio.ProjectSystem.Configuration;
using Gherkin;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;

namespace SpecFlow.VisualStudio.Editor.Completions
{
    public class DeveroomCompletionSource : DeveroomCompletionSourceBase
    {
        private readonly ITagAggregator<DeveroomTag> _tagAggregator;
        private readonly IIdeScope _ideScope;
        private readonly IProjectScope _project;

        public DeveroomCompletionSource(ITextBuffer buffer, ITagAggregator<DeveroomTag> tagAggregator, IIdeScope ideScope) 
            : base("Deveroom", buffer)
        {
            _tagAggregator = tagAggregator;
            _ideScope = ideScope;
            _project = ideScope.GetProject(buffer);
        }

        protected override KeyValuePair<SnapshotSpan, List<Completion>> CollectCompletions(SnapshotPoint triggerPoint)
        {
            var line = triggerPoint.GetContainingLine();
            IMappingTagSpan<DeveroomTag>[] tagSpans = _tagAggregator.GetTags(line.Extent).ToArray();
            var gherkinDocument = GetTagData<DeveroomGherkinDocument>(tagSpans, DeveroomTagTypes.Document);

            if (gherkinDocument == null)
                return GetDefaultKeywordCompletions(GetDefaultDialect(), triggerPoint);

            var gherkinDialect = gherkinDocument.GherkinDialect ?? GetDefaultDialect();
            var step = GetTagData<DeveroomGherkinStep>(tagSpans, DeveroomTagTypes.StepBlock);
            if (step != null && triggerPoint >= GetStepTextStart(step, line))
                return GetStepCompletions(step, triggerPoint);

            var tokens = gherkinDocument.GetExpectedTokens(line.LineNumber, _ideScope.MonitoringService);
            if (tokens.Length == 0)
                return GetDefaultKeywordCompletions(gherkinDialect, triggerPoint);

            var result = new List<Completion>();
            AddCompletionsFromExpectedTokens(tokens, result, gherkinDialect);
            return new KeyValuePair<SnapshotSpan, List<Completion>>(
                GetKeywordCompletionSpan(triggerPoint), 
                result);
        }

        private static SnapshotSpan GetKeywordCompletionSpan(SnapshotPoint triggerPoint)
        {
            var line = triggerPoint.GetContainingLine();
            var start = line.Start;
            while (start < triggerPoint && char.IsWhiteSpace(start.GetChar()))
                start += 1;
            var end = triggerPoint;
            // eat remaining parts of the current word
            while (end < line.End && !char.IsWhiteSpace(end.GetChar()))
                end += 1;
            // eat whitespaces after the current word
            while (end < line.End && char.IsWhiteSpace(end.GetChar()))
                end += 1;

            return new SnapshotSpan(start, end);
        }

        private T GetTagData<T>(IMappingTagSpan<DeveroomTag>[] tagSpans, string type) where T: class
        {
            return tagSpans.FirstOrDefault(ts => ts.Tag.Type == type)?.Tag?.Data as T;
        }

        private List<Completion> GetStepCompletions(DeveroomGherkinStep step)
        {
            var discoveryService = _project?.GetDiscoveryService();
            var bindingRegistry = discoveryService?.GetBindingRegistry();
            if (bindingRegistry == null)
                return new List<Completion>();

            var sampler = new StepDefinitionSampler();

            return bindingRegistry.StepDefinitions
                .Where(sd => sd.IsValid && sd.StepDefinitionType == step.ScenarioBlock)
                .Select(sd => new Completion(sampler.GetStepDefinitionSample(sd)))
                .ToList();
        }

        private KeyValuePair<SnapshotSpan, List<Completion>> GetStepCompletions(DeveroomGherkinStep step, SnapshotPoint triggerPoint)
        {
            return new KeyValuePair<SnapshotSpan, List<Completion>>(
                GetStepCompletionSpan(step, triggerPoint),
                GetStepCompletions(step));
        }

        private SnapshotSpan GetStepCompletionSpan(DeveroomGherkinStep step, SnapshotPoint triggerPoint)
        {
            var line = triggerPoint.GetContainingLine();
            var start = GetStepTextStart(step, line);

            return new SnapshotSpan(start, line.End);
        }

        private static SnapshotPoint GetStepTextStart(DeveroomGherkinStep step, ITextSnapshotLine line)
        {
            return line.Start + (step.Location.Column - 1) + step.Keyword.Length;
        }

        private void AddCompletionsFromExpectedTokens(TokenType[] expectedTokens, List<Completion> completions, GherkinDialect dialect)
        {
            foreach (var expectedToken in expectedTokens)
            {
                switch (expectedToken)
                {
                    case TokenType.FeatureLine:
                        AddCompletions(completions, dialect.FeatureKeywords, ": ", "Introduces the feature being described");
                        break;
                    case TokenType.RuleLine:
                        AddCompletions(completions, dialect.RuleKeywords, ": ", "Describes a business rule illustrated by the subsequent scenarios");
                        break;
                    case TokenType.BackgroundLine:
                        AddCompletions(completions, dialect.BackgroundKeywords, ": ", "Describes context common to all scenarios in this feature file");
                        break;
                    case TokenType.ScenarioLine:
                        AddCompletions(completions, dialect.ScenarioKeywords, ": ", "Illustrates a single system behaviour");
                        AddCompletions(completions, dialect.ScenarioOutlineKeywords, ": ", "A template for generating several, similar scenarios");
                        break;
                    case TokenType.ExamplesLine:
                        AddCompletions(completions, dialect.ExamplesKeywords, ": ", "A table of data used in conjunction with a scenario outline");
                        break;
                    case TokenType.StepLine:
                        AddCompletions(completions, RemoveBulletKeyword(dialect.GivenStepKeywords), description: "Describes the context for the behaviour");
                        AddCompletions(completions, RemoveBulletKeyword(dialect.WhenStepKeywords), description: "Describes the action that initiates the behaviour");
                        AddCompletions(completions, RemoveBulletKeyword(dialect.ThenStepKeywords), description: "Describes the expected outcome");
                        AddCompletions(completions, dialect.AndStepKeywords, description: "Used to combine steps in a readable format");
                        AddCompletions(completions, RemoveBulletKeyword(dialect.ButStepKeywords), description: "Used to combine steps in a readable format");
                        break;
                    case TokenType.DocStringSeparator:
                        AddCompletions(completions, new[] { "\"\"\"", "```" }, description: "Doc-string separator: Provides multi-line text parameter for the step");
                        break;
                    case TokenType.TableRow:
                        AddCompletions(completions, new[] { "| " }, description: "Data table and examples table cell separator");
                        break;
                    case TokenType.Language:
                        AddCompletions(completions, new[] { "#language: " }, description: "Specifies the language of the feature file");
                        break;
                    case TokenType.TagLine:
                        AddCompletions(completions, new []{ "@tag1 " }, description: "Labels a scenario, a feature or an examples block");
                        break;
                }
            }
        }

        private IEnumerable<string> RemoveBulletKeyword(string[] keywords)
        {
            return keywords.Where(k => !k.StartsWith("*"));
        }

        private void AddCompletions(List<Completion> completions, IEnumerable<string> keywords, string postfix = "", string description = null)
        {
            completions.AddRange(keywords.Select(keyword =>
            {
                var completion = new Completion(keyword + postfix);
                if (description != null)
                    completion.Description = description;
                return completion;
            }));
        }

        private GherkinDialect GetDefaultDialect()
        {
            var configuration = _ideScope.GetDeveroomConfiguration(_project);
            return SpecFlowGherkinDialectProvider.GetDialect(configuration.DefaultFeatureLanguage);
        }

        private List<Completion> GetDefaultKeywordCompletions(GherkinDialect gherkinDialect)
        {
            var result = new List<Completion>();
            AddDefaultKeywordCompletions(gherkinDialect, result);
            return result;
        }

        private KeyValuePair<SnapshotSpan, List<Completion>> GetDefaultKeywordCompletions(GherkinDialect gherkinDialect, SnapshotPoint triggerPoint)
        {
            return new KeyValuePair<SnapshotSpan, List<Completion>>(
                GetKeywordCompletionSpan(triggerPoint),
                GetDefaultKeywordCompletions(gherkinDialect));
        }

        private void AddDefaultKeywordCompletions(GherkinDialect gherkinDialect, List<Completion> completions)
        {
            foreach (var keyword in gherkinDialect.StepKeywords)
            {
                completions.Add(new Completion(keyword));
            }

            foreach (var blockKeyword in gherkinDialect.GetBlockKeywords())
            {
                completions.Add(new Completion(blockKeyword + ": "));
            }
        }
    }
}
