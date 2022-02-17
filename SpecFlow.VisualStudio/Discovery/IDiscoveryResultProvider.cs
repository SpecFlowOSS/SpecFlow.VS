namespace SpecFlow.VisualStudio.Discovery;

public interface IDiscoveryResultProvider
{
    DiscoveryResult RunDiscovery(string testAssemblyPath, string configFilePath, ProjectSettings projectSettings);
}

public class DiscoveryResultProvider : IDiscoveryResultProvider
{
    private readonly IMonitoringService _monitoringService;
    private readonly IProjectScope _projectScope;

    public DiscoveryResultProvider(IProjectScope projectScope, IMonitoringService monitoringService)
    {
        _projectScope = projectScope;
        _monitoringService = monitoringService;
    }

    private IDeveroomLogger Logger => _projectScope.IdeScope.Logger;

    public DiscoveryResult RunDiscovery(string testAssemblyPath, string configFilePath, ProjectSettings projectSettings)
    {
        if (projectSettings.SpecFlowVersion.Version.Major >= 3)
        {
            DiscoveryResult result = RunDiscovery(testAssemblyPath, configFilePath, projectSettings,
                OutProcSpecFlowConnectorFactory.CreateGeneric(_projectScope));

            if (!result.IsFailed)
                return result;
        }

        return RunDiscovery(testAssemblyPath, configFilePath, projectSettings, GetConnector(projectSettings));
    }

    public DiscoveryResult RunDiscovery(string testAssemblyPath, string configFilePath, ProjectSettings projectSettings,
        OutProcSpecFlowConnector connector) => connector.RunDiscovery(projectSettings.OutputAssemblyPath,
        projectSettings.SpecFlowConfigFilePath);

    private OutProcSpecFlowConnector GetConnector(ProjectSettings projectSettings) =>
        OutProcSpecFlowConnectorFactory.Create(_projectScope);
}
