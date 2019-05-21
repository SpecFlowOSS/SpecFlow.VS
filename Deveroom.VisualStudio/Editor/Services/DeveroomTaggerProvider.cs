using System;
using System.ComponentModel.Composition;
using System.Linq;
using Deveroom.VisualStudio.ProjectSystem;
using Deveroom.VisualStudio.SpecFlowVsCompatibility;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace Deveroom.VisualStudio.Editor.Services
{
    [Export(typeof(ITaggerProvider))]
    [ContentType("deveroom")]
    [TagType(typeof(DeveroomTag))]
    public class DeveroomTaggerProvider : ITaggerProvider
    {
        private readonly IIdeScope _ideScope;
        private readonly SpecFlowVsCompatibilityService _compatibilityService;

        internal bool CreateImmediateParsingTagger { get; set; } = false;

        [ImportingConstructor]
        public DeveroomTaggerProvider(IIdeScope ideScope, SpecFlowVsCompatibilityService compatibilityService)
        {
            _ideScope = ideScope;
            _compatibilityService = compatibilityService;
        }

        public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
        {
            _compatibilityService?.CheckCompatibilityOnce();
            return buffer.Properties.GetOrCreateSingletonProperty(creator: () => (ITagger<T>)new DeveroomTagger(buffer, _ideScope, CreateImmediateParsingTagger), key: typeof(DeveroomTagger));
        }

        public static DeveroomTagger GetDeveroomTagger(ITextBuffer buffer)
        {
            if (buffer.Properties.TryGetProperty<DeveroomTagger>(typeof(DeveroomTagger), out var tagger))
                return tagger;
            return null;
        }
    }
}
