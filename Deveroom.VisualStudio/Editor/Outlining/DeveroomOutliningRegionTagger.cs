using System;
using System.Collections.Generic;
using System.Linq;
using Deveroom.VisualStudio.Editor.Services;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;

namespace Deveroom.VisualStudio.Editor.Outlining
{
    internal class DeveroomOutliningRegionTagger : DeveroomTagConsumer, ITagger<IOutliningRegionTag>
    {
        public static readonly string[] OutlinedTags = {
            DeveroomTagTypes.ScenarioDefinitionBlock,
            DeveroomTagTypes.ExamplesBlock,
            DeveroomTagTypes.DataTable,
            DeveroomTagTypes.DocString
        };

        public DeveroomOutliningRegionTagger(ITextBuffer buffer, ITagAggregator<DeveroomTag> tagAggregator)
            : base(buffer, tagAggregator)
        {
        }

        public IEnumerable<ITagSpan<IOutliningRegionTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            return GetDeveroomTags(spans, t => OutlinedTags.Contains(t.Type))
                .Select(tagSpan => new TagSpan<IOutliningRegionTag>(GetOutliningSpan(tagSpan), 
                    CreateOutliningRegionTag(tagSpan.Key)));
        }

        private SnapshotSpan GetOutliningSpan(KeyValuePair<SnapshotSpan, DeveroomTag> tagSpan)
        {
            return new SnapshotSpan(tagSpan.Key.Start.GetContainingLine().End, tagSpan.Key.End);
        }

        private OutliningRegionTag CreateOutliningRegionTag(SnapshotSpan span)
        {
            return new OutliningRegionTag(false, false, "...", new OutliningHint(span));
        }

        class OutliningHint
        {
            private readonly Lazy<string> _hintText;

            public OutliningHint(SnapshotSpan span)
            {
                _hintText = new Lazy<string>(span.GetText);
            }

            public override string ToString()
            {
                return _hintText.Value;
            }
        }

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;
        protected override void RaiseChanged(SnapshotSpan span)
        {
            TagsChanged?.Invoke(this, new SnapshotSpanEventArgs(span));
        }
    }
}
