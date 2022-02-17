namespace SpecFlow.VisualStudio.Connectors;

public class GenericOutProcSpecFlowConnector : OutProcSpecFlowConnector
{
    private const string Connector = @"General-net6.0\specflow-vs.dll";

    public GenericOutProcSpecFlowConnector(
        DeveroomConfiguration configuration,
        IDeveroomLogger logger,
        TargetFrameworkMoniker targetFrameworkMoniker,
        string extensionFolder,
        ProcessorArchitectureSetting processorArchitecture,
        NuGetVersion specFlowVersion,
        IMonitoringService monitoringService)
        : base(
            configuration,
            logger,
            targetFrameworkMoniker,
            extensionFolder,
            processorArchitecture,
            specFlowVersion,
            monitoringService)
    {
    }

    protected override string GetConnectorPath(List<string> arguments)
    {
        var connectorsFolder = GetConnectorsFolder();
        return GetDotNetExecCommand(arguments, connectorsFolder, Connector);
    }
}
