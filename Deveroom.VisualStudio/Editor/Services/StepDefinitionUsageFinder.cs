using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using Deveroom.VisualStudio.Configuration;
using Deveroom.VisualStudio.Diagonostics;
using Deveroom.VisualStudio.Discovery;
using Deveroom.VisualStudio.Editor.Services.Parser;
using Deveroom.VisualStudio.Monitoring;
using Gherkin.Ast;

namespace Deveroom.VisualStudio.Editor.Services
{
    public class StepDefinitionUsage
    {
        public SourceLocation SourceLocation { get; }
        public Step Step { get; }

        public StepDefinitionUsage(SourceLocation sourceLocation, Step step)
        {
            SourceLocation = sourceLocation;
            Step = step;
        }

        public override string ToString()
        {
            return $"{SourceLocation}";
        }
    }

    public class StepDefinitionUsageFinder
    {
        private readonly IFileSystem _fileSystem;
        private readonly IDeveroomLogger _logger;
        private readonly IMonitoringService _monitoringService;

        public StepDefinitionUsageFinder(IFileSystem fileSystem, IDeveroomLogger logger, IMonitoringService monitoringService)
        {
            _fileSystem = fileSystem;
            _logger = logger;
            _monitoringService = monitoringService;
        }

        public IEnumerable<StepDefinitionUsage> FindUsages(ProjectStepDefinitionBinding[] stepDefinitions, string[] featureFiles, DeveroomConfiguration configuration)
        {
            return featureFiles.SelectMany(ff => FindUsages(stepDefinitions, ff, configuration));
        }

        private IEnumerable<StepDefinitionUsage> FindUsages(ProjectStepDefinitionBinding[] stepDefinitions, string featureFilePath, DeveroomConfiguration configuration)
        {
            var featureFileContent = LoadContent(featureFilePath);
            if (featureFileContent == null)
                return Enumerable.Empty<StepDefinitionUsage>();

            return FindUsagesFromContent(stepDefinitions, featureFileContent, featureFilePath, configuration);
        }

        private string LoadContent(string featureFilePath)
        {
            try
            {
                return _fileSystem.File.ReadAllText(featureFilePath);
            }
            catch (Exception ex)
            {
                _logger.LogDebugException(ex);
                return null;
            }
        }

        class UsageFinderContext : IGherkinDocumentContext
        {
            public IGherkinDocumentContext Parent { get; }
            public object Node { get; }

            public UsageFinderContext(object node, IGherkinDocumentContext parent = null)
            {
                Node = node;
                Parent = parent;
            }
        }

        public IEnumerable<StepDefinitionUsage> FindUsagesFromContent(ProjectStepDefinitionBinding[] stepDefinitions,
            string featureFileContent, string featureFilePath, DeveroomConfiguration configuration)
        {
            var dialectProvider = SpecFlowGherkinDialectProvider.Get(configuration.DefaultFeatureLanguage);
            var parser = new DeveroomGherkinParser(dialectProvider, _monitoringService);
            parser.ParseAndCollectErrors(featureFileContent, _logger, 
                out var gherkinDocument, out _);

            var featureNode = gherkinDocument?.Feature;
            if (featureNode == null)
                yield break;

            var dummyRegistry = new ProjectBindingRegistry
            {
                StepDefinitions = stepDefinitions
            };

            var featureContext = new UsageFinderContext(featureNode);

            foreach (var scenarioDefinition in featureNode.FlattenStepsContainers())
            {
                var context = new UsageFinderContext(scenarioDefinition, featureContext);
                foreach (var step in scenarioDefinition.Steps)
                {
                    var matchResult = dummyRegistry.MatchStep(step, context);
                    if (matchResult == null)
                        continue; // this will not happen
                    if (matchResult.HasDefined)
                        yield return new StepDefinitionUsage(
                            GetSourceLocation(step, featureFilePath), step);
                }
            }
        }

        private SourceLocation GetSourceLocation(Step step, string featureFilePath)
        {
            return new SourceLocation(featureFilePath, step.Location.Line, step.Location.Column);
        }
    }
}
