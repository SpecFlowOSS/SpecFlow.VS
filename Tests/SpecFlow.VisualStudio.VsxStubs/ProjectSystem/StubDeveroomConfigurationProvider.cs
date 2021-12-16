using System;
using SpecFlow.VisualStudio.Configuration;

namespace SpecFlow.VisualStudio.VsxStubs.ProjectSystem;

public class StubDeveroomConfigurationProvider : IDeveroomConfigurationProvider
{
    private readonly DeveroomConfiguration _configuration;

    public StubDeveroomConfigurationProvider(DeveroomConfiguration configuration)
    {
        _configuration = configuration;
    }

    public event EventHandler<EventArgs> WeakConfigurationChanged
    {
        add { }
        remove { }
    }

    public DeveroomConfiguration GetConfiguration() => _configuration;
}
