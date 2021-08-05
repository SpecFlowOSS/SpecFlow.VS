using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SpecFlow.VisualStudio.Connectors;
using SpecFlow.VisualStudio.Diagnostics;
using SpecFlow.VisualStudio.ProjectSystem;
using SpecFlow.VisualStudio.ProjectSystem.Configuration;
using SpecFlow.VisualStudio.ProjectSystem.Settings;
using SpecFlow.VisualStudio.SpecFlowConnector.Models;

namespace SpecFlow.VisualStudio.Discovery
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
