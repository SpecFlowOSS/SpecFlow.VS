using System;
using System.Linq;

namespace SpecFlow.VisualStudio.ProjectSystem
{
    public interface IDeveroomWindowManager
    {
        bool? ShowDialog<TViewModel>(TViewModel viewModel);
    }
}
