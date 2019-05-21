using System;
using Deveroom.VisualStudio.Editor.Services;
using Deveroom.VisualStudio.VsxStubs.ProjectSystem;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;

namespace Deveroom.VisualStudio.VsxStubs
{
    public class StubBufferTagAggregatorFactoryService : IBufferTagAggregatorFactoryService
    {
        private readonly StubIdeScope _ideScope;

        public StubBufferTagAggregatorFactoryService(StubIdeScope ideScope)
        {
            _ideScope = ideScope;
        }

        public ITagAggregator<T> CreateTagAggregator<T>(ITextBuffer textBuffer) where T : ITag
        {
            return CreateTagAggregator<T>(textBuffer, TagAggregatorOptions.None);
        }

        public ITagAggregator<T> CreateTagAggregator<T>(ITextBuffer textBuffer, TagAggregatorOptions options) where T : ITag
        {
            if (typeof(T) == typeof(DeveroomTag))
            {
                var taggerProvider = new DeveroomTaggerProvider(_ideScope, null);
                taggerProvider.CreateImmediateParsingTagger = true;

                return new StubTagAggregator<T>((ITagger<T>)taggerProvider.CreateTagger<DeveroomTag>(textBuffer), VsxStubObjects.BufferGraphFactoryService.CreateBufferGraph(textBuffer));
            }

            throw new NotSupportedException();
        }
    }
}
