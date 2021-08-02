using System.Collections.Generic;
using SpecFlow.VisualStudio.Configuration;
using SpecFlow.VisualStudio.Discovery;
using Microsoft.VisualStudio.Text;

namespace SpecFlow.VisualStudio.Editor.Services
{
    public interface IDeveroomTagParser
    {
        ICollection<DeveroomTag> Parse(ITextSnapshot fileSnapshot, ProjectBindingRegistry bindingRegistry, DeveroomConfiguration configuration);
    }
}