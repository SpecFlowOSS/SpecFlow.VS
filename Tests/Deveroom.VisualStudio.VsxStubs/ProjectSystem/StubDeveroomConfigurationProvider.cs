using System;
using Deveroom.VisualStudio.Configuration;
using Deveroom.VisualStudio.ProjectSystem.Configuration;

namespace Deveroom.VisualStudio.VsxStubs.ProjectSystem
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
