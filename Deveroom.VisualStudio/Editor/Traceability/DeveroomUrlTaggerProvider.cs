using System;
using System.ComponentModel.Composition;
using System.Linq;
using Deveroom.VisualStudio.Editor.Services;
using Deveroom.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace Deveroom.VisualStudio.Editor.Traceability
{
    [Export(typeof(ITaggerProvider))]
    [ContentType("deveroom")]
    [TagType(typeof(UrlTag))]
    public class DeveroomUrlTaggerProvider : ITaggerProvider
    {
        private readonly IBufferTagAggregatorFactoryService _aggregatorFactory;
        private readonly IIdeScope _ideScope;

        [ImportingConstructor]
        public DeveroomUrlTaggerProvider(IBufferTagAggregatorFactoryService aggregatorFactory, IIdeScope ideScope)
        {
            _aggregatorFactory = aggregatorFactory;
            _ideScope = ideScope;
        }

        public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
        {
            var tagAggregator = _aggregatorFactory.CreateTagAggregator<DeveroomTag>(buffer);
            return buffer.Properties.GetOrCreateSingletonProperty(creator: () => (ITagger<T>)new DeveroomUrlTagger(buffer, tagAggregator, _ideScope));
        }
    }
}
