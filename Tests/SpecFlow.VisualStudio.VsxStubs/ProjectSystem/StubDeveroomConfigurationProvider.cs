using System;
using SpecFlow.VisualStudio.Configuration;
using SpecFlow.VisualStudio.ProjectSystem.Configuration;

namespace SpecFlow.VisualStudio.VsxStubs.ProjectSystem
{
    public class StubDeveroomConfigurationProvider : IDeveroomConfigurationProvider
    {
        private readonly DeveroomConfiguration _configuration;

        public event EventHandler<EventArgs> WeakConfigurationChanged { add { } remove { } }

        public StubDeveroomConfigurationProvider(DeveroomConfiguration configuration)
        {
            _configuration = configuration;
        }

        public DeveroomConfiguration GetConfiguration()
        {
            return _configuration;
        }
    }
}
