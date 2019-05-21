using System;
using System.Linq;
using System.Collections.Generic;
using Deveroom.VisualStudio.Configuration;
using Deveroom.VisualStudio.Diagonostics;
using Deveroom.VisualStudio.Discovery;
using Deveroom.VisualStudio.ProjectSystem;
using Deveroom.VisualStudio.ProjectSystem.Configuration;
using Deveroom.VisualStudio.ProjectSystem.Settings;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;

namespace Deveroom.VisualStudio.Editor.Services
{
    public class DeveroomTagger : ITagger<DeveroomTag>, IDisposable
    {
        class TagsCache
        {
            public int SnapshotVersion { get; }
            public int? BindingRegistryVersion { get; }
            public DateTime ConfigurationChangeTime { get; }
            public ICollection<DeveroomTag> Tags { get; }

            public TagsCache(ITextSnapshot snapshot, int? bindingRegistryVersion, DateTime configurationChangeTime, ICollection<DeveroomTag> tags)
            {
                SnapshotVersion = snapshot.Version.VersionNumber;
                Tags = tags;
                BindingRegistryVersion = bindingRegistryVersion;
                ConfigurationChangeTime = configurationChangeTime;
            }

            public TagsCache()
            {
                SnapshotVersion = -1;
                BindingRegistryVersion = null;
                Tags = new List<DeveroomTag>();
            }
        }

        private readonly ITextBuffer _buffer;
        private readonly IIdeScope _ideScope;
        private readonly IDeveroomTagParser _deveroomTagParser;
        private readonly IDiscoveryService _discoveryService;
        private readonly IDeveroomConfigurationProvider _deveroomConfigurationProvider;
        private readonly ProjectSettingsProvider _projectSettingsProvider;
        private readonly ActionThrottler _actionThrottler;
        private readonly CalculationCache<TagsCache> _tagsCache = new CalculationCache<TagsCache>();

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

        public DeveroomTagger(ITextBuffer buffer, IIdeScope ideScope, bool immediateParsing)
        {
            _buffer = buffer;
            _ideScope = ideScope;
            var project = ideScope.GetProject(buffer);

            _deveroomConfigurationProvider = ideScope.GetDeveroomConfigurationProvider(project);
            _projectSettingsProvider = project?.GetProjectSettingsProvider();
            _discoveryService = project?.GetDiscoveryService();

            InitializeWithDiscoveryService(ideScope, project);

            _deveroomTagParser = new DeveroomTagParser(ideScope.Logger, ideScope.MonitoringService);
            _actionThrottler = new ActionThrottler(() =>
            {
                if (ideScope.IsSolutionLoaded)
                    ReCalculate(buffer.CurrentSnapshot);
                else
                    _actionThrottler.TriggerAction(forceDelayed: true);
            });

            if (immediateParsing)
                _tagsCache.Invalidate();
            else
            {
                _tagsCache.ReCalculate(() => new TagsCache()); //empty valid result
                _actionThrottler.TriggerAction(forceDelayed: true);
            }

            SubscribeToEvents();
        }

        private void InitializeWithDiscoveryService(IIdeScope ideScope, IProjectScope project)
        {
            var projectSettings = _projectSettingsProvider?.GetProjectSettings();
            if (projectSettings != null && projectSettings.IsSpecFlowLibProject)
            {
                // this is the first feature file in the project
                var updatedProjectSettings = _projectSettingsProvider.CheckProjectSettings();
                if (updatedProjectSettings.IsSpecFlowTestProject)
                    _discoveryService?.CheckBindingRegistry();
            }

            ideScope.Logger.LogVerbose($"Creating DeveroomTagger (project: {project}, SpecFlow: {projectSettings?.GetSpecFlowVersionLabel() ?? "n/a"})");
            ideScope.MonitoringService.MonitorOpenFeatureFile(projectSettings);
        }

        private void SubscribeToEvents()
        {
            _deveroomConfigurationProvider.WeakConfigurationChanged += OnContextChanged;
            if (_discoveryService != null)
                _discoveryService.WeakBindingRegistryChanged += OnContextChanged;
            _buffer.Changed += OnBufferChanged;
        }

        private void OnBufferChanged(object sender, TextContentChangedEventArgs e)
        {
            _actionThrottler.TriggerAction();
        }

        private void OnContextChanged(object sender, EventArgs e)
        {
            TriggerInvalidate();
        }

        private void TriggerInvalidate()
        {
            _tagsCache.Invalidate(true);
            RaiseTagsChanged();
            var fileSnapshot = _buffer.CurrentSnapshot;
            TagsChanged?.Invoke(this, new SnapshotSpanEventArgs(new SnapshotSpan(fileSnapshot, 0, fileSnapshot.Length)));
        }

        public IEnumerable<ITagSpan<DeveroomTag>> GetTags(NormalizedSnapshotSpanCollection spans, bool forceUpToDate)
        {
            _tagsCache.Invalidate();
            return GetTags(spans);
        }

        public IEnumerable<ITagSpan<DeveroomTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            var snapshot = spans[0].Snapshot;
            var tags = GetTagsForFile();

            if (tags == null)
                yield break;

            foreach (SnapshotSpan queriedSpan in spans)
            {
                foreach (var tag in tags)
                {
                    var tagSpan = tag.Span.TranslateTo(snapshot, SpanTrackingMode.EdgeInclusive);
                    if (tagSpan.IntersectsWith(queriedSpan))
                        yield return new TagSpan<DeveroomTag>(tagSpan, tag);

                    if (tagSpan.Start > queriedSpan.End)
                        break;
                }
            }
        }

        private ICollection<DeveroomTag> GetTagsForFile()
        {
            if (_tagsCache.IsInvalid)
                _actionThrottler.TriggerAction(forceDirect: true);

            var currentTagsCache = _tagsCache.Value;
            return currentTagsCache?.Tags;
        }

        private void ReCalculate(ITextSnapshot fileSnapshot)
        {
            var recalculated = _tagsCache.ReCalculate((() =>
            {
                var configuration = _deveroomConfigurationProvider.GetConfiguration();
                var bindingRegistry = _discoveryService?.GetBindingRegistry();
                var bindingRegistryVersion = bindingRegistry?.Version;
                var currentTagsCache = _tagsCache.Value;
                if (currentTagsCache != null &&
                    currentTagsCache.SnapshotVersion == fileSnapshot.Version.VersionNumber &&
                    currentTagsCache.BindingRegistryVersion == bindingRegistryVersion &&
                    currentTagsCache.ConfigurationChangeTime == configuration.ConfigurationChangeTime)
                    return currentTagsCache;

                var deveroomTags = ReParse(fileSnapshot, bindingRegistry, configuration);
                MonitorParse(deveroomTags);
                return new TagsCache(fileSnapshot, bindingRegistryVersion, configuration.ConfigurationChangeTime, deveroomTags);
            }));

            if (recalculated)
                RaiseTagsChanged(fileSnapshot);
        }

        private int _trackParseCounter = 0;
        private const int TRACK_PARSES_BY = 10;
        private void MonitorParse(List<DeveroomTag> deveroomTags)
        {
            if (deveroomTags == null)
                return;
            var counter = _trackParseCounter++;
            if (counter % TRACK_PARSES_BY == 0)
            {
                var scenarioDefinitionCount = deveroomTags.Count(t => t.Type == DeveroomTagTypes.ScenarioDefinitionBlock);
                if (scenarioDefinitionCount > 0 || deveroomTags.Any(t => t.Type == DeveroomTagTypes.FeatureBlock))
                {
                    // valid feature file
                    _ideScope.MonitoringService.MonitorParserParse(counter, scenarioDefinitionCount);
                }
                else
                {
                    // skipping parses of totally invalid feature files (there is not even a feature header)
                    _trackParseCounter--;
                }
            }
        }

        private void RaiseTagsChanged(ITextSnapshot fileSnapshot = null)
        {
            fileSnapshot = fileSnapshot ?? _buffer.CurrentSnapshot;
            TagsChanged?.Invoke(this, new SnapshotSpanEventArgs(new SnapshotSpan(fileSnapshot, 0, fileSnapshot.Length)));
        }

        private List<DeveroomTag> ReParse(ITextSnapshot fileSnapshot, ProjectBindingRegistry bindingRegistry, DeveroomConfiguration configuration)
        {
            var tags = new List<DeveroomTag>(_deveroomTagParser.Parse(fileSnapshot, bindingRegistry, configuration));
            tags.Sort((t1, t2) => t1.Span.Start.Position.CompareTo(t2.Span.Start.Position));
            return tags;
        }

        public void Dispose()
        {
            // unfortunately VS will not call the dispose, therefore we have to use weak events
            // but this is what we should have done on dispose

            _deveroomConfigurationProvider.WeakConfigurationChanged -= OnContextChanged;
            if (_discoveryService != null)
                _discoveryService.WeakBindingRegistryChanged -= OnContextChanged;
            _buffer.Changed -= OnBufferChanged;
        }
    }
}
