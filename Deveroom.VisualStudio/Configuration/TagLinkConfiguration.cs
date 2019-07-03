using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Deveroom.VisualStudio.Common;
using Equ;

namespace Deveroom.VisualStudio.Configuration
{
    public class TagLinkConfiguration : MemberwiseEquatable<TagLinkConfiguration>
    {
        public string TagPattern { get; set; }
        public string UrlTemplate { get; set; }

        internal Regex ResolvedTagPattern { get; private set; }

        private void FixEmptyContainers()
        {
            //nop;
        }

        public void CheckConfiguration()
        {
            FixEmptyContainers();

            if (string.IsNullOrEmpty(TagPattern))
                throw new DeveroomConfigurationException("'traceability/tagLinks[]/tagPattern' must be specified");
            if (string.IsNullOrEmpty(UrlTemplate))
                throw new DeveroomConfigurationException("'traceability/tagLinks[]/urlTemplate' must be specified");

            try
            {
                ResolvedTagPattern = new Regex("^" + TagPattern.TrimStart('^').TrimEnd('$') + "$");
            }
            catch (Exception e)
            {
                throw new DeveroomConfigurationException($"Invalid regular expression '{TagPattern}' was specified as 'traceability/tagLinks[]/tagPattern': {e.Message}");
            }
        }
    }
}
