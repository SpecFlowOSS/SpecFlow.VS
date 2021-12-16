namespace SpecFlow.VisualStudio.ProjectSystem.Configuration;

internal class ConfigCache
{
    public ConfigCache(DeveroomConfiguration configuration, ConfigSource[] configSources)
    {
        Configuration = configuration;
        ConfigSources = configSources;
    }

    public DeveroomConfiguration Configuration { get; }
    public ConfigSource[] ConfigSources { get; }
}
