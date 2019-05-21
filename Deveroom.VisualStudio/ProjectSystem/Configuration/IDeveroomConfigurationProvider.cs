using System;
using Deveroom.VisualStudio.Configuration;

namespace Deveroom.VisualStudio.ProjectSystem.Configuration
{
    public interface IDeveroomConfigurationProvider
    {
        event EventHandler<EventArgs> WeakConfigurationChanged;
        DeveroomConfiguration GetConfiguration();
    }
}