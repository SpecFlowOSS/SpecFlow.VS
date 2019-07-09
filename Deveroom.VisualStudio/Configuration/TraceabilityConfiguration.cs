using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Deveroom.VisualStudio.Configuration
{
    public class TraceabilityConfiguration
    {
        public TagLinkConfiguration[] TagLinks { get; set; } = new TagLinkConfiguration[0];

        private void FixEmptyContainers()
        {
            TagLinks = TagLinks ?? new TagLinkConfiguration[0];
        }

        public void CheckConfiguration()
        {
            FixEmptyContainers();

            foreach (var tagLinkConfiguration in TagLinks)
            {
                tagLinkConfiguration.CheckConfiguration();
            }
        }

        #region Equality

        protected bool Equals(TraceabilityConfiguration other)
        {
            return Equals(TagLinks, other.TagLinks);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((TraceabilityConfiguration) obj);
        }

        public override int GetHashCode()
        {
            return (TagLinks != null ? TagLinks.GetHashCode() : 0);
        }

        #endregion
    }
}
