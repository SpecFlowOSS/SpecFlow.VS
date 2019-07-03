using System;
using System.Collections.Generic;
using System.Linq;
using Deveroom.VisualStudio.Editor.Services;
using Gherkin.Ast;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;

namespace Deveroom.VisualStudio.Editor.Traceability
{
    internal class DeveroomUrlTagger : DeveroomTagConsumer, ITagger<UrlTag>
    {
        public DeveroomUrlTagger(ITextBuffer buffer, ITagAggregator<DeveroomTag> tagAggregator)
            : base(buffer, tagAggregator)
        {
        }

        public IEnumerable<ITagSpan<UrlTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            return GetDeveroomTags(spans, t => t.Type == DeveroomTagTypes.Tag)
                .Select(tagSpan => new { tagSpan.Key, Url = GetUrl((Tag)tagSpan.Value.Data) })
                .Where(urlSpan => urlSpan.Url != null)
                .Select(urlSpan => new TagSpan<UrlTag>(urlSpan.Key,
                    new UrlTag(urlSpan.Url)));
        }

        private Uri GetUrl(Tag tag)
        {
            return null;
        }

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;
        protected override void RaiseChanged(SnapshotSpan span)
        {
            TagsChanged?.Invoke(this, new SnapshotSpanEventArgs(span));
        }
    }
}