namespace SpecFlow.VisualStudio.Editor.Services;

public class FeatureFileTagger : ITagger<DeveroomTag>
{
    private readonly IDiscoveryService _discoveryService;
    private readonly ConcurrentDictionary<SnapshotSpan, IEnumerable<ITagSpan<DeveroomTag>>> _getTagsCache = new();
    private readonly IDeveroomLogger _logger;
    private readonly IDeveroomTagParser _tagParser;
    private readonly ITextBuffer2 _textBuffer;

    private ITextSnapshot _currentSnapshot;
    private IReadOnlyCollection<DeveroomTag> _parsedTags;
    public int ParsedSnapshotVersionNumber = int.MinValue;

    public FeatureFileTagger(
        IDiscoveryService discoveryService,
        IDeveroomLogger logger,
        IDeveroomTagParser tagParser,
        ITextBuffer2 textBuffer)
    {
        _tagParser = tagParser;
        _textBuffer = textBuffer;
        _logger = logger;
        _discoveryService = discoveryService;
        _parsedTags = ImmutableArray<DeveroomTag>.Empty;
        _currentSnapshot = textBuffer.CurrentSnapshot;
        textBuffer.ChangedOnBackground += TextBuffer_ChangedOnBackground;
        discoveryService.WeakBindingRegistryChanged += BindingRegistryChanged;
    }

    public IEnumerable<ITagSpan<DeveroomTag>> GetUpToDateTags(SnapshotSpan span)
    {
        if (ParsedSnapshotVersionNumber < span.Snapshot.Version.VersionNumber)
        {
            using var tagsChanged = new TagsChangedSubscriber(this, _logger);
            tagsChanged.Wait(span.Snapshot.Version.VersionNumber);
        }

        var normalizedCaretSpan = new NormalizedSnapshotSpanCollection(span);
        return GetTags(normalizedCaretSpan);
    }

    public IEnumerable<ITagSpan<DeveroomTag>> GetTags(NormalizedSnapshotSpanCollection spans)
    {
        foreach (SnapshotSpan queriedSpan in spans)
        {
            IEnumerable<ITagSpan<DeveroomTag>> tagSpans = BuildTags(queriedSpan);
            if (_getTagsCache.TryGetValue(queriedSpan, out var cached))
                tagSpans = cached;

            foreach (var tagSpan in tagSpans)
                yield return tagSpan;
        }
    }

    public event EventHandler<SnapshotSpanEventArgs> TagsChanged = null!;

    private void BindingRegistryChanged(object sender, EventArgs e)
    {
        if (_textBuffer.ContentType.IsOfType(VsContentTypes.FeatureFile))
        {
            ForceReparseOnBackground();
        }
        else
        {
            _discoveryService.WeakBindingRegistryChanged -= BindingRegistryChanged;
            _textBuffer.ChangedOnBackground -= TextBuffer_ChangedOnBackground;
            _textBuffer.Properties.RemoveProperty(typeof(ITagger<DeveroomTag>));
        }
    }

    private IEnumerable<ITagSpan<DeveroomTag>> BuildTags(SnapshotSpan queriedSpan)
    {
        var generatedTags = new List<ITagSpan<DeveroomTag>>();
        foreach (var tag in _parsedTags)
        {
            SnapshotSpan tagSpan = tag.Span.TranslateTo(queriedSpan.Snapshot, SpanTrackingMode.EdgeInclusive);
            if (tagSpan.IntersectsWith(queriedSpan))
            {
                var tagSpans = new TagSpan<DeveroomTag>(tagSpan, tag);
                generatedTags.Add(tagSpans);
                yield return tagSpans;
            }

            if (tagSpan.Start > queriedSpan.End)
                break;
        }

        _getTagsCache.TryAdd(queriedSpan, generatedTags);
    }

    private void TextBuffer_ChangedOnBackground(object sender, EventArgs e)
    {
        var snapshot = _textBuffer.CurrentSnapshot;
        if (_currentSnapshot.Version.VersionNumber >= snapshot.Version.VersionNumber)
            return;

        _currentSnapshot = snapshot;

        Parse(snapshot);
    }

    public void ForceReparseOnBackground()
        => Task.Run(() => Parse(_textBuffer.CurrentSnapshot));

    private void Parse(ITextSnapshot snapshot)
    {
        _parsedTags = _tagParser.Parse(snapshot);
        ParsedSnapshotVersionNumber = snapshot.Version.VersionNumber;
        _getTagsCache.Clear();
        TagsChanged?.Invoke(this, new SnapshotSpanEventArgs(new SnapshotSpan(snapshot, 0, snapshot.Length)));
    }
}
