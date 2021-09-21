using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SpecFlow.VisualStudio.Editor.Classification;
using SpecFlow.VisualStudio.Editor.Services;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace SpecFlow.VisualStudio.Editor.Errors
{
    [Export(typeof(ITaggerProvider))]
    [ContentType(VsContentTypes.FeatureFile)]
    [TagType(typeof(ErrorTag))]
    class DeveroomErrorTaggerProvider : ITaggerProvider
    {
        private readonly IBufferTagAggregatorFactoryService _aggregatorFactory;

        [ImportingConstructor]
        public DeveroomErrorTaggerProvider(IBufferTagAggregatorFactoryService aggregatorFactory)
        {
            _aggregatorFactory = aggregatorFactory;
        }

        public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
        {
            var tagAggregator = _aggregatorFactory.CreateTagAggregator<DeveroomTag>(buffer);
            return buffer.Properties.GetOrCreateSingletonProperty(creator: () => (ITagger<T>)new DeveroomErrorTagger(buffer, tagAggregator));
        }
    }
}
