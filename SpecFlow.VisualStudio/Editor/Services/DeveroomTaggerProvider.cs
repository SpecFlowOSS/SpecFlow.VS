#nullable disable

namespace SpecFlow.VisualStudio.Editor.Services;

[Export(typeof(ITaggerProvider))]
[ContentType(VsContentTypes.FeatureFile)]
[TagType(typeof(DeveroomTag))]
public class DeveroomTaggerProvider : ITaggerProvider
{
    private readonly IIdeScope _ideScope;

    [ImportingConstructor]
    public DeveroomTaggerProvider(IIdeScope ideScope)
    {
        _ideScope = ideScope;
    }

    internal bool CreateImmediateParsingTagger { get; set; } = false;

    public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
    {
        return buffer.Properties.GetOrCreateSingletonProperty(
            creator: () =>
                (ITagger<T>) new DeveroomTagger(buffer, _ideScope, CreateImmediateParsingTagger,
                    new ActionThrottlerFactory()), key: typeof(DeveroomTagger));
    }

    public static DeveroomTagger GetDeveroomTagger(ITextBuffer buffer, IIdeScope ideScope)
    {
        if (buffer.Properties.TryGetProperty<DeveroomTagger>(typeof(DeveroomTagger), out var tagger))
            return tagger;
        var deveroomTagger = new DeveroomTaggerProvider(ideScope).CreateTagger<DeveroomTag>(buffer) as DeveroomTagger;
        deveroomTagger.InvalidateCache();
        return deveroomTagger;
    }
}
