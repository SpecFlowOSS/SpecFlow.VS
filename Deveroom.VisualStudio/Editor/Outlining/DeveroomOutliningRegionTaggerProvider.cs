using System;
using System.ComponentModel.Composition;
using System.Linq;
using Deveroom.VisualStudio.Editor.Services;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace Deveroom.VisualStudio.Editor.Outlining
{
    [Export(typeof(ITaggerProvider))]
    [ContentType("deveroom")]
    [TagType(typeof(IOutliningRegionTag))]
    public class DeveroomOutliningRegionTaggerProvider : ITaggerProvider
    {
        private readonly IBufferTagAggregatorFactoryService _aggregatorFactory;

        [ImportingConstructor]
        public DeveroomOutliningRegionTaggerProvider(IBufferTagAggregatorFactoryService aggregatorFactory)
        {
            _aggregatorFactory = aggregatorFactory;
        }

        public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
        {
            var tagAggregator = _aggregatorFactory.CreateTagAggregator<DeveroomTag>(buffer);
            return buffer.Properties.GetOrCreateSingletonProperty(creator: () => (ITagger<T>)new DeveroomOutliningRegionTagger(buffer, tagAggregator));
        }
    }
}
