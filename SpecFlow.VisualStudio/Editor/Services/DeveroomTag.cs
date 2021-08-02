using System;
using System.Collections.Generic;
using System.Linq;
using SpecFlow.VisualStudio.Discovery;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;

namespace SpecFlow.VisualStudio.Editor.Services
{
    public class DeveroomTag : ITag, IGherkinDocumentContext
    {
        private readonly List<DeveroomTag> _childTags = new List<DeveroomTag>();

        public string Type { get; }
        public SnapshotSpan Span { get; }
        public object Data { get; }

        public DeveroomTag ParentTag { get; private set; }
        public ICollection<DeveroomTag> ChildTags => _childTags;
        public bool IsError => Type.EndsWith("Error");

        IGherkinDocumentContext IGherkinDocumentContext.Parent => ParentTag;
        object IGherkinDocumentContext.Node => Data;

        public DeveroomTag(string type, SnapshotSpan span, object data = null)
        {
            Type = type;
            Span = span;
            Data = data;
        }

        internal DeveroomTag AddChild(DeveroomTag childTag)
        {
            childTag.ParentTag = this;
            _childTags.Add(childTag);
            return childTag;
        }

        public override string ToString()
        {
            return $"{Type}:{Span}";
        }

        public IEnumerable<DeveroomTag> GetDescendantsOfType(string type)
        {
            foreach (var childTag in ChildTags)
            {
                if (childTag.Type == type)
                    yield return childTag;

                foreach (var descendantTag in childTag.GetDescendantsOfType(type))
                {
                    yield return descendantTag;
                }
            }
        }
    }
}
