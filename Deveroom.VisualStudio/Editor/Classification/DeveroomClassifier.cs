using System;
using System.Collections.Generic;
using Deveroom.VisualStudio.Editor.Services;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Tagging;

namespace Deveroom.VisualStudio.Editor.Classification
{
    internal class DeveroomClassifier : DeveroomTagConsumer, IClassifier
    {
        private static readonly Dictionary<string, string> TagClassificationTypeMapping = new Dictionary<string, string>
        {
            { DeveroomTagTypes.StepKeyword, DeveroomClassifications.Keyword },
            { DeveroomTagTypes.DefinitionLineKeyword, DeveroomClassifications.Keyword },
            { DeveroomTagTypes.Tag, DeveroomClassifications.Tag },
            { DeveroomTagTypes.Description, DeveroomClassifications.Description },
            { DeveroomTagTypes.Comment, DeveroomClassifications.Comment },
            { DeveroomTagTypes.DocString, DeveroomClassifications.DocString },
            { DeveroomTagTypes.DataTable, DeveroomClassifications.DataTable },
            { DeveroomTagTypes.UndefinedStep, DeveroomClassifications.UndefinedStep },
            { DeveroomTagTypes.StepParameter, DeveroomClassifications.StepPatameter },
            { DeveroomTagTypes.ScenarioOutlinePlaceholder, DeveroomClassifications.ScenarioOutlinePlaceholder },
            { DeveroomTagTypes.DataTableHeader, DeveroomClassifications.ScenarioOutlinePlaceholder },
        };

        private readonly IClassificationTypeRegistryService _classificationTypeRegistry;

        internal DeveroomClassifier(IClassificationTypeRegistryService classificationTypeRegistry, ITextBuffer buffer, ITagAggregator<DeveroomTag> tagAggregator)
            : base(buffer, tagAggregator)
        {
            _classificationTypeRegistry = classificationTypeRegistry;
        }

        public event EventHandler<ClassificationChangedEventArgs> ClassificationChanged;
        protected override void RaiseChanged(SnapshotSpan span)
        {
            ClassificationChanged?.Invoke(this, new ClassificationChangedEventArgs(span));
        }

        public IList<ClassificationSpan> GetClassificationSpans(SnapshotSpan span)
        {
            var classifications = new List<ClassificationSpan>();

            foreach (var tagSpan in GetDeveroomTags(span))
            {
                AddClassificationForTag(classifications, tagSpan.Value, tagSpan.Key);
            }

            return classifications;
        }

        private void AddClassificationForTag(List<ClassificationSpan> classifications, DeveroomTag tag, SnapshotSpan span)
        {
            if (TagClassificationTypeMapping.TryGetValue(tag.Type, out var classification))
            {
                classifications.Add(new ClassificationSpan(span, _classificationTypeRegistry.GetClassificationType(classification)));
            }
        }
    }
}
