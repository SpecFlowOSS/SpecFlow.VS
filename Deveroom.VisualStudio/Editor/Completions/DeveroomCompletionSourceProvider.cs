using System;
using System.ComponentModel.Composition;
using System.Linq;
using Deveroom.VisualStudio.Editor.Services;
using Deveroom.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace Deveroom.VisualStudio.Editor.Completions
{
    [Export(typeof(ICompletionSourceProvider))]
    [ContentType("deveroom")]
    [Name("Deveroom Gherkin completion")]
    public class DeveroomCompletionSourceProvider : ICompletionSourceProvider
    {
        private readonly IBufferTagAggregatorFactoryService _aggregatorFactory;
        private readonly IIdeScope _ideScope;

        [ImportingConstructor]
        public DeveroomCompletionSourceProvider(IBufferTagAggregatorFactoryService aggregatorFactory, IIdeScope ideScope)
        {
            _aggregatorFactory = aggregatorFactory;
            _ideScope = ideScope;
        }

        public ICompletionSource TryCreateCompletionSource(ITextBuffer textBuffer)
        {
            var tagAggregator = _aggregatorFactory.CreateTagAggregator<DeveroomTag>(textBuffer);
            return new DeveroomCompletionSource(textBuffer, tagAggregator, _ideScope);
        }
    }
}
