using System;
using SpecFlow.VisualStudio.Configuration;

namespace SpecFlow.VisualStudio.ProjectSystem.Configuration
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