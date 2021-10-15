using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using Gherkin.Ast;
using SpecFlow.VisualStudio.SpecFlowConnector.Models;

namespace SpecFlow.VisualStudio.Discovery
{
    public class ProjectBindingRegistry
    {
        public static ProjectBindingRegistry Invalid = new(Array.Empty<ProjectStepDefinitionBinding>());
        private const string DataTableDefaultTypeName = TypeShortcuts.SpecFlowTableType;
        private const string DocStringDefaultTypeName = TypeShortcuts.StringType;

        private static int _versionCounter = 0;

        public ProjectBindingRegistry(IEnumerable<ProjectStepDefinitionBinding> stepDefinitions)
        {
            StepDefinitions = stepDefinitions.ToImmutableArray();
        }

        public int Version { get; } = Interlocked.Increment(ref _versionCounter);
        public ImmutableArray<ProjectStepDefinitionBinding> StepDefinitions { get; }
        public bool IsFailed => this == Invalid;

        public MatchResult MatchStep(Step step, IGherkinDocumentContext context = null)
        {
            if (IsFailed)
                return null;

            var stepText = step.Text;
            if (context.IsScenarioOutline() && stepText.Contains("<"))
            {
                var stepsWithScopes = GherkinDocumentContextCalculator.GetScenarioOutlineStepsWithContexts(step, context);
                return MatchMultiScope(step, stepsWithScopes);
            }
            if (context.IsBackground())
            {
                var stepsWithScopes = GherkinDocumentContextCalculator.GetBackgroundStepsWithContexts(step, context);
                return MatchMultiScope(step, stepsWithScopes);
            }

            return MatchResult.CreateMultiMatch(MatchSingleContextResult(step, context));
        }

        private MatchResult MatchMultiScope(Step step, IEnumerable<KeyValuePair<string, IGherkinDocumentContext>> stepsWithScopes)
        {
            var matches = stepsWithScopes.Select(swc => MatchSingleContextResult(step, swc.Value, swc.Key))
                .SelectMany(m => m).ToArray();
            var multiMatches = MergeMultiMatches(matches);
            Debug.Assert(multiMatches.Length > 0); // MatchSingleContextResult returns undefined steps as well
            return MatchResult.CreateMultiMatch(multiMatches);
        }

        private MatchResultItem[] MergeMultiMatches(MatchResultItem[] matches)
        {
            var multiMatches = matches.GroupBy(m => m.Type).SelectMany(g =>
            {
                switch (g.Key)
                {
                    case MatchResultType.Undefined:
                        return new[] {g.First()};
                    case MatchResultType.Ambiguous:
                    case MatchResultType.Defined:
                        return MergeSingularMatchResults(g);
                    default:
                        throw new InvalidOperationException();
                }
            }).ToArray();
            return multiMatches;
        }

        private IEnumerable<MatchResultItem> MergeSingularMatchResults(IEnumerable<MatchResultItem> results)
        {
            foreach (var implGroup in results.GroupBy(r => r.MatchedStepDefinition.Implementation))
            {
                // yielding the first with error or just the first if there were no errors
                yield return implGroup.FirstOrDefault(mri => mri.HasErrors) ?? implGroup.First();
            }
        }

        private MatchResultItem[] MatchSingleContextResult(Step step, IGherkinDocumentContext context, string stepText = null)
        {
            stepText = stepText ?? step.Text;
            var sdMatches = StepDefinitions.Select(sd => sd.Match(step, context, stepText)).Where(m => m != null).ToArray();
            if (!sdMatches.Any())
                return new[] {MatchResultItem.CreateUndefined(step, stepText)};

            sdMatches = HandleDataTableOverloads(step, sdMatches);
            sdMatches = HandleDocStringOverloads(step, sdMatches);
            sdMatches = HandleArgumentlessOverloads(step, sdMatches);
            sdMatches = HandleScopeOverloads(sdMatches);

            if (sdMatches.Length == 1)
                return new[] { sdMatches[0] };

            return sdMatches.Select(mi => mi.CloneToAmbiguousItem()).ToArray();
        }

        /// <summary>
        /// Selects DataTable overload, this can be eliminated later when we process conversions
        /// </summary>
        private MatchResultItem[] HandleDataTableOverloads(Step step, MatchResultItem[] sdMatches)
        {
            if (step.Argument is DataTable && sdMatches.Length > 1)
            {
                // assuming that sdMatches contains real matches, not match candidates (hints)
                Debug.Assert(sdMatches.All(m => m.Type == MatchResultType.Defined));
                var matchesWithDataTableParameter = sdMatches.Where(m =>
                    m.ParameterMatch.DataTableParameterType == DataTableDefaultTypeName).ToArray();
                if (matchesWithDataTableParameter.Any())
                    sdMatches = matchesWithDataTableParameter;
            }
            return sdMatches;
        }

        /// <summary>
        /// Selects DocString overload, this can be eliminated later when we process conversions
        /// </summary>
        private MatchResultItem[] HandleDocStringOverloads(Step step, MatchResultItem[] sdMatches)
        {
            if (step.Argument is DocString && sdMatches.Length > 1)
            {
                // assuming that sdMatches contains real matches, not match candidates (hints)
                Debug.Assert(sdMatches.All(m => m.Type == MatchResultType.Defined));
                var matchesWithDocStringParameter = sdMatches.Where(m =>
                    m.ParameterMatch.DocStringParameterType == DocStringDefaultTypeName).ToArray();
                if (matchesWithDocStringParameter.Any())
                    sdMatches = matchesWithDocStringParameter;
            }
            return sdMatches;
        }

        /// <summary>
        /// Selects argumentless overload, this can be eliminated later when we process conversions(?)
        /// </summary>
        private MatchResultItem[] HandleArgumentlessOverloads(Step step, MatchResultItem[] sdMatches)
        {
            if (step.Argument == null && sdMatches.Length > 1)
            {
                // assuming that sdMatches contains real matches, not match candidates (hints)
                Debug.Assert(sdMatches.All(m => m.Type == MatchResultType.Defined));

                var matchesWithoutParameterError = sdMatches.Where(m => !m.ParameterMatch.HasError).ToArray();
                if (matchesWithoutParameterError.Length == 1)
                {
                    var candidatingMatch = matchesWithoutParameterError[0];
                    if (sdMatches.All(m => m == candidatingMatch ||
                                           m.ParameterMatch.ParameterTypes.Length ==
                                           m.ParameterMatch.StepTextParameters.Length + 1))
                        return matchesWithoutParameterError;
                }
            }
            return sdMatches;
        }

        /// <summary>
        /// Selects scoped overload
        /// </summary>
        private MatchResultItem[] HandleScopeOverloads(MatchResultItem[] sdMatches)
        {
            if (sdMatches.Length > 1)
            {
                // assuming that sdMatches contains real matches, not match candidates (hints)
                Debug.Assert(sdMatches.All(m => m.Type == MatchResultType.Defined));
                var matchesWithScope = sdMatches.Where(m =>
                    m.MatchedStepDefinition.Scope != null).ToArray();
                if (matchesWithScope.Any())
                    sdMatches = matchesWithScope;
            }
            return sdMatches;
        }

        public ProjectBindingRegistry AddStepDefinition(ProjectStepDefinitionBinding sd)
        {
            var stepDefinitions = StepDefinitions.ToList();
            stepDefinitions.Add(sd);
            return new ProjectBindingRegistry(stepDefinitions);
        }

        public ProjectBindingRegistry AddStepDefinitions(IEnumerable<ProjectStepDefinitionBinding> projectStepDefinitionBindings)
        {
            var stepDefinitions = StepDefinitions.ToList();
            stepDefinitions.AddRange(projectStepDefinitionBindings);
            return new ProjectBindingRegistry(stepDefinitions);
        }

        public ProjectBindingRegistry ReplaceStepDefinition(ProjectStepDefinitionBinding original, ProjectStepDefinitionBinding replacement)
        {
            return new ProjectBindingRegistry(StepDefinitions.Select(sd => sd == original ? replacement : sd));
        }

        public ProjectBindingRegistry Where(Func<ProjectStepDefinitionBinding, bool> predicate)
        {
            return new ProjectBindingRegistry(StepDefinitions.Where(predicate));
        }
    }
}
