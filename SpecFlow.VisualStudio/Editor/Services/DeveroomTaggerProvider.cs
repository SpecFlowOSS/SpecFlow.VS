using System;
using System.ComponentModel.Composition;
using System.Linq;
using SpecFlow.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace SpecFlow.VisualStudio.Editor.Services
{
    [Export(typeof(ITaggerProvider))]
    [ContentType(VsContentTypes.FeatureFile)]
    [TagType(typeof(DeveroomTag))]
    public class DeveroomTaggerProvider : ITaggerProvider
    {
        private readonly IIdeScope _ideScope;

        internal bool CreateImmediateParsingTagger { get; set; } = false;

        [ImportingConstructor]
        public DeveroomTaggerProvider(IIdeScope ideScope)
        {
            _ideScope = ideScope;
        }

        public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
        {
            return buffer.Properties.GetOrCreateSingletonProperty(creator: () => (ITagger<T>)new DeveroomTagger(buffer, _ideScope, CreateImmediateParsingTagger, new ActionThrottlerFactory()), key: typeof(DeveroomTagger));
        }

        public static DeveroomTagger GetDeveroomTagger(ITextBuffer buffer)
        {
            if (buffer.Properties.TryGetProperty<DeveroomTagger>(typeof(DeveroomTagger), out var tagger))
                return tagger;
            return null;
        }
    }
}
