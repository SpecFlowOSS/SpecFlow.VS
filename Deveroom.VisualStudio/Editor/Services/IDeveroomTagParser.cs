using System.Collections.Generic;
using Deveroom.VisualStudio.Configuration;
using Deveroom.VisualStudio.Discovery;
using Microsoft.VisualStudio.Text;

namespace Deveroom.VisualStudio.Editor.Services
{
    public interface IDeveroomTagParser
    {
        ICollection<DeveroomTag> Parse(ITextSnapshot fileSnapshot, ProjectBindingRegistry bindingRegistry, DeveroomConfiguration configuration);
    }
}