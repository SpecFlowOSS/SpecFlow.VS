using System;
using Deveroom.VisualStudio.Configuration;

namespace Deveroom.VisualStudio.ProjectSystem.Configuration
{
    public class ProjectSystemDeveroomConfigurationProvider : IDeveroomConfigurationProvider
    {
        private readonly DeveroomConfiguration _configuration;

        public event EventHandler<EventArgs> WeakConfigurationChanged { add { } remove { } }

        public ProjectSystemDeveroomConfigurationProvider(IIdeScope ideScope)
        {
            _configuration = new DeveroomConfiguration(); //TODO: Load solution-level config
        }

        public DeveroomConfiguration GetConfiguration() => _configuration;
    }
}