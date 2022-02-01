using System.Runtime.Loader;

namespace SpecFlow.VisualStudio.SpecFlowConnector;

public class DiscoveryCommand : ICommand
{
    public const string CommandName = "discovery";
    private readonly DiscoveryOptions _options;
    private readonly ILogger _log;

    public DiscoveryCommand(DiscoveryOptions options, ILogger log)
    {
        _options = options;
        _log = log;
    }

    public CommandResult Execute(Func<string, Assembly> assemblyFromPath)
    {
        var assembly = assemblyFromPath(_options.AssemblyFile.FullName);
        return new("{}");
    }
}
