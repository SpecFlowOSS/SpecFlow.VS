using System.ComponentModel.Composition;
using SpecFlow.VisualStudio.Editor.Services;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace SpecFlow.VisualStudio.Editor.Classification
{
    [Export(typeof(IClassifierProvider))]
    [ContentType("deveroom")]
    internal class DeveroomClassifierProvider : IClassifierProvider
    {
#pragma warning disable 649

        [Import]
        private IClassificationTypeRegistryService _classificationRegistry;
        [Import]
        private IBufferTagAggregatorFactoryService _aggregatorFactory;

#pragma warning restore 649

        public IClassifier GetClassifier(ITextBuffer buffer)
        {
            var tagAggregator = _aggregatorFactory.CreateTagAggregator<DeveroomTag>(buffer);

            return buffer.Properties.GetOrCreateSingletonProperty(creator: () => new DeveroomClassifier(this._classificationRegistry, buffer, tagAggregator));
        }
    }
}
