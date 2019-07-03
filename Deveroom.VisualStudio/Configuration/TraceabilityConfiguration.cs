using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Equ;

namespace Deveroom.VisualStudio.Configuration
{
    public class TraceabilityConfiguration : MemberwiseEquatable<TraceabilityConfiguration>
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
    }
}
