using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using Deveroom.VisualStudio.Configuration;
using Deveroom.VisualStudio.Diagonostics;
using Deveroom.VisualStudio.Discovery;
using Deveroom.VisualStudio.Editor.Services.Parser;
using Deveroom.VisualStudio.Monitoring;
using Gherkin.Ast;
using Microsoft.VisualStudio.Text;

namespace Deveroom.VisualStudio.Editor.Services
{
    public class DeveroomTagParser : IDeveroomTagParser
    {
        private readonly IDeveroomLogger _logger;
        private readonly IMonitoringService _monitoringService;

        public DeveroomTagParser(IDeveroomLogger logger, IMonitoringService monitoringService)
        {
            _logger = logger;
            _monitoringService = monitoringService;
        }

        public ICollection<DeveroomTag> Parse(ITextSnapshot fileSnapshot, ProjectBindingRegistry bindingRegistry, DeveroomConfiguration configuration)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            try
            {
                return ParseInternal(fileSnapshot, bindingRegistry, configuration);
            }
            catch (Exception ex)
            {
                _logger.LogException(_monitoringService, ex, "Unhandled parsing error");
                return new List<DeveroomTag>();
            }
            finally
            {
                stopwatch.Stop();
                _logger.LogVerbose($"Parsed buffer v{fileSnapshot.Version.VersionNumber} in {stopwatch.ElapsedMilliseconds}ms on thread {Thread.CurrentThread.ManagedThreadId}");
            }
        }

        private ICollection<DeveroomTag> ParseInternal(ITextSnapshot fileSnapshot, ProjectBindingRegistry bindingRegistry, DeveroomConfiguration deveroomConfiguration)
        {
            var dialectProvider = SpecFlowGherkinDialectProvider.Get(deveroomConfiguration.DefaultFeatureLanguage);
            var parser = new DeveroomGherkinParser(dialectProvider, _monitoringService);

            parser.ParseAndCollectErrors(fileSnapshot.GetText(), _logger,
                out var gherkinDocument, out var parserErrors);

            var result = new List<DeveroomTag>();

            if (gherkinDocument != null)
                AddGherkinDocumentTags(fileSnapshot, bindingRegistry, gherkinDocument, result);

            foreach (var parserException in parserErrors)
            {
                var line = GetSnapshotLine(parserException.Location, fileSnapshot);
                var startPoint = GetColumnPoint(line, parserException.Location);
                var span = new SnapshotSpan(startPoint, line.End);

                result.Add(new DeveroomTag(DeveroomTagTypes.ParserError,
                    span, parserException.Message));
            }

            return result;
        }

        private void AddGherkinDocumentTags(ITextSnapshot fileSnapshot, ProjectBindingRegistry bindingRegistry,
            DeveroomGherkinDocument gherkinDocument, List<DeveroomTag> result)
        {
            var documentTag = new DeveroomTag(DeveroomTagTypes.Document, new SnapshotSpan(fileSnapshot, 0, fileSnapshot.Length), gherkinDocument);
            result.Add(documentTag);

            if (gherkinDocument.Feature != null)
            {
                var featureTag = GetFeatureTags(fileSnapshot, bindingRegistry, gherkinDocument.Feature);
                result.AddRange(GetAllTags(featureTag));
            }

            if (gherkinDocument.Comments != null)
                foreach (var comment in gherkinDocument.Comments)
                {
                    result.Add(new DeveroomTag(DeveroomTagTypes.Comment,
                        GetTextSpan(fileSnapshot, comment.Location, comment.Text)));
                }
        }

        private DeveroomTag GetFeatureTags(ITextSnapshot fileSnapshot, ProjectBindingRegistry bindingRegistry, Feature feature)
        {
            var featureTag = CreateDefinitionBlockTag(feature, DeveroomTagTypes.FeatureBlock, fileSnapshot,
                fileSnapshot.LineCount);

            foreach (var scenarioDefinition in feature.StepsContainers())
            {
                var scenarioDefinitionTag = CreateDefinitionBlockTag(scenarioDefinition,
                    DeveroomTagTypes.ScenarioDefinitionBlock, fileSnapshot,
                    GetScenarioDefinitionLastLine(scenarioDefinition), featureTag);

                foreach (var step in scenarioDefinition.Steps)
                {
                    var stepTag = scenarioDefinitionTag.AddChild(new DeveroomTag(DeveroomTagTypes.StepBlock,
                        GetBlockSpan(fileSnapshot, step.Location, GetStepLastLine(step)), step));

                    stepTag.AddChild(
                        new DeveroomTag(DeveroomTagTypes.StepKeyword,
                            GetTextSpan(fileSnapshot, step.Location, step.Keyword),
                            step.Keyword));

                    if (step.Argument is DataTable dataTable)
                    {
                        stepTag.AddChild(
                            new DeveroomTag(DeveroomTagTypes.DataTable,
                                GetBlockSpan(fileSnapshot, dataTable.Rows.First().Location,
                                    dataTable.Rows.Last().Location.Line),
                                dataTable));
                    }
                    else if (step.Argument is DocString docString)
                    {
                        stepTag.AddChild(
                            new DeveroomTag(DeveroomTagTypes.DocString,
                                GetBlockSpan(fileSnapshot, docString.Location,
                                    GetStepLastLine(step)),
                                docString));
                    }

                    if (scenarioDefinition is ScenarioOutline)
                    {
                        AddPlaceholderTags(fileSnapshot, stepTag, step);
                    }

                    var match = bindingRegistry?.MatchStep(step, scenarioDefinitionTag);
                    if (match != null)
                    {
                        if (match.HasDefined || match.HasAmbiguous)
                        {
                            stepTag.AddChild(new DeveroomTag(DeveroomTagTypes.DefinedStep,
                                GetTextSpan(fileSnapshot, step.Location, step.Text, offset: step.Keyword.Length),
                                match));
                            if (!(scenarioDefinition is ScenarioOutline) || !step.Text.Contains("<"))
                            {
                                var parameterMatch = match.Items.FirstOrDefault(m => m.ParameterMatch != null)
                                    ?.ParameterMatch;
                                AddParameterTags(fileSnapshot, parameterMatch, stepTag, step);
                            }
                        }

                        if (match.HasUndefined)
                        {
                            stepTag.AddChild(new DeveroomTag(DeveroomTagTypes.UndefinedStep,
                                GetTextSpan(fileSnapshot, step.Location, step.Text, offset: step.Keyword.Length),
                                match));
                        }

                        if (match.HasErrors)
                        {
                            stepTag.AddChild(new DeveroomTag(DeveroomTagTypes.BindingError,
                                GetTextSpan(fileSnapshot, step.Location, step.Text, offset: step.Keyword.Length),
                                match.GetErrorMessage()));
                        }
                    }
                }

                if (scenarioDefinition is ScenarioOutline scenarioOutline)
                {
                    foreach (var scenarioOutlineExample in scenarioOutline.Examples)
                    {
                        var examplesBlockTag = CreateDefinitionBlockTag(scenarioOutlineExample,
                            DeveroomTagTypes.ExamplesBlock, fileSnapshot,
                            GetExamplesLastLine(scenarioOutlineExample), scenarioDefinitionTag);
                        if (scenarioOutlineExample.TableHeader != null)
                        {
                            foreach (var cell in scenarioOutlineExample.TableHeader.Cells)
                            {
                                examplesBlockTag.AddChild(new DeveroomTag(DeveroomTagTypes.ScenarioOutlinePlaceholder,
                                    GetSpan(fileSnapshot, cell.Location, cell.Value.Length, offset: 0),
                                    cell));
                            }
                        }
                    }
                }
            }

            return featureTag;
        }

        private void AddParameterTags(ITextSnapshot fileSnapshot, ParameterMatch parameterMatch, DeveroomTag stepTag, Step step)
        {
            foreach (var parameter in parameterMatch.StepTextParameters)
            {
                stepTag.AddChild(new DeveroomTag(DeveroomTagTypes.StepParameter,
                    GetSpan(fileSnapshot, step.Location, parameter.Length, offset: step.Keyword.Length + parameter.Index),
                    parameter));
            }
        }

        private void AddPlaceholderTags(ITextSnapshot fileSnapshot, DeveroomTag stepTag, Step step)
        {
            var placeholders = MatchedScenarioOutlinePlaceholder.MatchScenarioOutlinePlaceholders(step);
            foreach (var placeholder in placeholders)
            {
                stepTag.AddChild(new DeveroomTag(DeveroomTagTypes.ScenarioOutlinePlaceholder,
                    GetSpan(fileSnapshot, step.Location, placeholder.Length, offset: step.Keyword.Length + placeholder.Index),
                    placeholder));
            }
        }

        private DeveroomTag CreateDefinitionBlockTag(IHasDescription astNode, string tagType, ITextSnapshot fileSnapshot, int lastLine, DeveroomTag parentTag = null)
        {
            var span = GetBlockSpan(fileSnapshot, ((IHasLocation)astNode).Location, lastLine);
            var blockTag = new DeveroomTag(tagType, span, astNode);
            parentTag?.AddChild(blockTag);
            blockTag.AddChild(CreateDefinitionLineKeyword(fileSnapshot, astNode));
            if (astNode is IHasTags hasTags)
            {
                foreach (var gherkinTag in hasTags.Tags)
                {
                    blockTag.AddChild(
                        new DeveroomTag(DeveroomTagTypes.Tag,
                            GetTextSpan(fileSnapshot, gherkinTag.Location, gherkinTag.Name)));
                }
            }

            if (!string.IsNullOrEmpty(astNode.Description))
            {
                var startLineNumber = ((IHasLocation) astNode).Location.Line + 1;
                while (string.IsNullOrWhiteSpace(fileSnapshot.GetLineFromLineNumber(GetSnapshotLineNumber(startLineNumber, fileSnapshot)).GetText()))
                {
                    startLineNumber++;
                }
                blockTag.AddChild(
                    new DeveroomTag(DeveroomTagTypes.Description,
                        GetBlockSpan(fileSnapshot, startLineNumber,
                            CountLines(astNode.Description))));
            }

            return blockTag;
        }

        private static readonly Regex NewLineRe = new Regex(@"(\r\n|\n|\r)");
        private int CountLines(string text) =>
            NewLineRe.Matches(text).Count + 1;

        private DeveroomTag CreateDefinitionLineKeyword(ITextSnapshot fileSnapshot, IHasDescription hasDescription) =>
            new DeveroomTag(DeveroomTagTypes.DefinitionLineKeyword,
                GetTextSpan(fileSnapshot, ((IHasLocation)hasDescription).Location, hasDescription.Keyword, 1));

        private IEnumerable<DeveroomTag> GetAllTags(DeveroomTag tag)
        {
            yield return tag;
            foreach (var childTag in tag.ChildTags)
            foreach (var allChildTag in GetAllTags(childTag))
                yield return allChildTag;
        }

        private int GetScenarioDefinitionLastLine(StepsContainer stepsContainer)
        {
            if (stepsContainer is ScenarioOutline scenarioOutline)
            {
                var lastExamples = scenarioOutline.Examples.LastOrDefault();
                if (lastExamples != null)
                {
                    return GetExamplesLastLine(lastExamples);
                }
            }

            var lastStep = stepsContainer.Steps.LastOrDefault();
            if (lastStep == null)
                return stepsContainer.Location.Line;
            return GetStepLastLine(lastStep);
        }

        private static int GetExamplesLastLine(Examples examples)
        {
            var lastRow = examples.TableBody?.LastOrDefault() ?? examples.TableHeader;
            if (lastRow != null)
                return lastRow.Location.Line;
            return examples.Location.Line;
        }

        private int GetStepLastLine(Step step)
        {
            if (step.Argument is DocString docStringArg)
            {
                int lineCount = CountLines(docStringArg.Content);
                return docStringArg.Location.Line + lineCount - 1 + 2;
            }
            if (step.Argument is DataTable dataTable)
            {
                return dataTable.Rows.Last().Location.Line;
            }
            return step.Location.Line;
        }

        private SnapshotSpan GetBlockSpan(ITextSnapshot snapshot, Location startLocation, int locationEndLine)
        {
            var startLine = GetSnapshotLine(startLocation, snapshot);
            var endLine = snapshot.GetLineFromLineNumber(GetSnapshotLineNumber(locationEndLine, snapshot));

            return new SnapshotSpan(startLine.Start, endLine.End);
        }

        private SnapshotSpan GetBlockSpan(ITextSnapshot snapshot, int startLineNumber, int lineCount)
        {
            var startLine = snapshot.GetLineFromLineNumber(GetSnapshotLineNumber(startLineNumber, snapshot));
            var endLine = snapshot.GetLineFromLineNumber(GetSnapshotLineNumber(startLineNumber + lineCount - 1, snapshot));

            return new SnapshotSpan(startLine.Start, endLine.End);
        }

        private SnapshotSpan GetTextSpan(ITextSnapshot snapshot, Location location, string text, int extraLength = 0, int offset = 0)
        {
            return GetSpan(snapshot, location, text.Length + extraLength, offset);
        }

        private SnapshotSpan GetSpan(ITextSnapshot snapshot, Location location, int length, int offset = 0)
        {
            var line = GetSnapshotLine(location, snapshot);
            var startPoint = GetColumnPoint(line, location);
            if (offset != 0)
                startPoint = startPoint.Add(offset);
            return new SnapshotSpan(startPoint, length);
        }

        private int GetSnapshotLineNumber(Location location, ITextSnapshot snapshot) =>
            GetSnapshotLineNumber(location.Line, snapshot);

        private int GetSnapshotLineNumber(int locationLine, ITextSnapshot snapshot) =>
            locationLine == 0
                ? 0 // global error
                : locationLine - 1 >= snapshot.LineCount
                    ? snapshot.LineCount - 1 // unexpected end of file
                    : locationLine - 1;

        private int GetSnapshotColumn(Location location) =>
            location.Column == 0
                ? 0 // whole line error
                : location.Column - 1;

        private SnapshotPoint GetColumnPoint(ITextSnapshotLine line, Location location) =>
            line.Start.Add(GetSnapshotColumn(location));

        private ITextSnapshotLine GetSnapshotLine(Location location, ITextSnapshot snapshot) =>
            snapshot.GetLineFromLineNumber(GetSnapshotLineNumber(location, snapshot));
    }
}
