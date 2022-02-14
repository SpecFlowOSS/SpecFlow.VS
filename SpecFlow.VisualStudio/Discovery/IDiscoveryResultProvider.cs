namespace SpecFlow.VisualStudio.Discovery;

public interface IDiscoveryResultProvider
{
    DiscoveryResult RunDiscovery(string testAssemblyPath, string configFilePath, ProjectSettings projectSettings);
}

public class DiscoveryResultProvider : IDiscoveryResultProvider
{
    private readonly IProjectScope _projectScope;
    private readonly IMonitoringService _monitoringService;

    public DiscoveryResultProvider(IProjectScope projectScope, IMonitoringService monitoringService)
    {
        _projectScope = projectScope;
        _monitoringService = monitoringService;
    }

    private IDeveroomLogger Logger => _projectScope.IdeScope.Logger;

    public DiscoveryResult RunDiscovery(string testAssemblyPath, string configFilePath, ProjectSettings projectSettings)
    {
        OutProcSpecFlowConnector connector = OutProcSpecFlowConnectorFactory.CreateGeneric(_projectScope);
        DiscoveryResult result = RunDiscovery(testAssemblyPath, configFilePath, projectSettings, connector);

        _monitoringService.TransmitEvent(new DiscoveryResultEvent(result));

        if (!result.IsFailed)
            return result;

        Logger.LogWarning(result.ErrorMessage);

        connector = GetConnector(projectSettings);
        result = RunDiscovery(testAssemblyPath, configFilePath, projectSettings, connector);

        return result;
    }

    public DiscoveryResult RunDiscovery(string testAssemblyPath, string configFilePath, ProjectSettings projectSettings,
        OutProcSpecFlowConnector connector) => connector.RunDiscovery(projectSettings.OutputAssemblyPath,
        projectSettings.SpecFlowConfigFilePath);

    private OutProcSpecFlowConnector GetConnector(ProjectSettings projectSettings) =>
        OutProcSpecFlowConnectorFactory.Create(_projectScope);
}
