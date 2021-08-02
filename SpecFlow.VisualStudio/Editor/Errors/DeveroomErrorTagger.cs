using System;
using System.Collections.Generic;
using System.Linq;
using SpecFlow.VisualStudio.Editor.Services;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Tagging;

namespace SpecFlow.VisualStudio.Editor.Errors
{
    internal class DeveroomErrorTagger : DeveroomTagConsumer, ITagger<ErrorTag>
    {
        public DeveroomErrorTagger(ITextBuffer buffer, ITagAggregator<DeveroomTag> tagAggregator)
            : base(buffer, tagAggregator)
        {
        }

        public IEnumerable<ITagSpan<ErrorTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            return GetDeveroomTags(spans, t => t.IsError)
                .Select(tagSpan => new TagSpan<ErrorTag>(tagSpan.Key, 
                    new ErrorTag(PredefinedErrorTypeNames.SyntaxError, tagSpan.Value.Data?.ToString())));
        }

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;
        protected override void RaiseChanged(SnapshotSpan span)
        {
            TagsChanged?.Invoke(this, new SnapshotSpanEventArgs(span));
        }
    }
}