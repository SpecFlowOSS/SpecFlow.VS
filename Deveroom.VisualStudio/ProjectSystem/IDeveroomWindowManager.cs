using System;
using System.Linq;

namespace Deveroom.VisualStudio.ProjectSystem
{
    public interface IDeveroomWindowManager
    {
        bool? ShowDialog<TViewModel>(TViewModel viewModel);
    }
}
