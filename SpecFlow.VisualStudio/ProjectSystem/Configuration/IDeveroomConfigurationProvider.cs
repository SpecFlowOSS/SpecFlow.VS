using System;

namespace SpecFlow.VisualStudio.ProjectSystem.Configuration;

public interface IDeveroomConfigurationProvider
{
    event EventHandler<EventArgs> WeakConfigurationChanged;
    DeveroomConfiguration GetConfiguration();
}
