using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Deveroom.VisualStudio.Connectors;
using Deveroom.VisualStudio.Diagonostics;
using Deveroom.VisualStudio.ProjectSystem;
using Deveroom.VisualStudio.ProjectSystem.Configuration;
using Deveroom.VisualStudio.ProjectSystem.Settings;
using Deveroom.VisualStudio.SpecFlowConnector.Models;

namespace Deveroom.VisualStudio.Discovery
{
    public interface IDiscoveryResultProvider
    {
        DiscoveryResult RunDiscovery(string testAssemblyPath, string configFilePath, ProjectSettings projectSettings);
    }

    public class DiscoveryResultProvider : IDiscoveryResultProvider
    {
        private readonly IProjectScope _projectScope;

        public DiscoveryResultProvider(IProjectScope projectScope)
        {
            _projectScope = projectScope;
        }

        private IDeveroomLogger Logger => _projectScope.IdeScope.Logger;

        public DiscoveryResult RunDiscovery(string testAssemblyPath, string configFilePath, ProjectSettings projectSettings)
        {
            var connector = GetConnector(projectSettings);
            return connector.RunDiscovery(projectSettings.OutputAssemblyPath, projectSettings.SpecFlowConfigFilePath);
        }

        private OutProcSpecFlowConnector GetConnector(ProjectSettings projectSettings)
        {
            return OutProcSpecFlowConnectorFactory.Create(_projectScope);
        }
    }
}
