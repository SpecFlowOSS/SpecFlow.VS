using System;
using Microsoft.VisualStudio.Text.Tagging;
using SpecFlow.VisualStudio.Editor.Services;

namespace SpecFlow.VisualStudio.VsxStubs;

public class StubBufferTagAggregatorFactoryService : IBufferTagAggregatorFactoryService
{
    private readonly StubIdeScope _ideScope;

    public StubBufferTagAggregatorFactoryService(StubIdeScope ideScope)
    {
        _ideScope = ideScope;
    }

    public ITagAggregator<T> CreateTagAggregator<T>(ITextBuffer textBuffer) where T : ITag =>
        CreateTagAggregator<T>(textBuffer, TagAggregatorOptions.None);

    public ITagAggregator<T> CreateTagAggregator<T>(ITextBuffer textBuffer, TagAggregatorOptions options) where T : ITag
    {
        if (typeof(T) == typeof(DeveroomTag))
        {
            var taggerProvider = new DeveroomTaggerProvider(_ideScope);
            taggerProvider.CreateImmediateParsingTagger = true;

            return new StubTagAggregator<T>((ITagger<T>) taggerProvider.CreateTagger<DeveroomTag>(textBuffer),
                VsxStubObjects.BufferGraphFactoryService.CreateBufferGraph(textBuffer));
        }

        throw new NotSupportedException();
    }
}
