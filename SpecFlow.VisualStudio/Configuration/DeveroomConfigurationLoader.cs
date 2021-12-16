#nullable disable
using System.Linq.Expressions;

namespace SpecFlow.VisualStudio.Configuration;

public class DeveroomConfigurationLoader
{
    private readonly IConfigDeserializer<DeveroomConfiguration> _configDeserializer;

    private DeveroomConfigurationLoader(IConfigDeserializer<DeveroomConfiguration> configDeserializer)
    {
        _configDeserializer = configDeserializer;
    }

    public static DeveroomConfigurationLoader CreateSpecFlowJsonConfigurationLoader() =>
        new(new SpecFlowConfigDeserializer());

    public static DeveroomConfigurationLoader CreateDeveroomJsonConfigurationLoader() =>
        new(new JsonNetConfigDeserializer<DeveroomConfiguration>());

    public DeveroomConfiguration Load(string configFilePath)
    {
        var config = new DeveroomConfiguration();
        Update(config, configFilePath);
        return config;
    }

    public void Update(DeveroomConfiguration config, string configFilePath)
    {
        if (!File.Exists(configFilePath))
            throw new DeveroomConfigurationException($"The specified config file '{configFilePath}' does not exist.");
        var configFolder = Path.GetDirectoryName(configFilePath) ??
                           throw new DeveroomConfigurationException(
                               $"The specified config file '{configFilePath}' does not contain a folder.");

        var jsonString = File.ReadAllText(configFilePath);
        Update(config, jsonString, configFolder);
    }

    public void Update(DeveroomConfiguration config, string configFileContent, string configFolder)
    {
        _configDeserializer.Populate(configFileContent, config);

        config.ConfigurationBaseFolder = configFolder;

        config.SpecFlow.ConfigFilePath = EnsureFullPath(config, c => c.SpecFlow.ConfigFilePath);
        config.SpecFlow.GeneratorFolder = EnsureFullPath(config, c => c.SpecFlow.GeneratorFolder, true);
    }

    private string ExpandEnvironmentVariables(string value)
    {
        if (value == null)
            return null;
        return Environment.ExpandEnvironmentVariables(value);
    }

    private string EnsureFullPath(DeveroomConfiguration config, string filePath, string label, bool isFolder = false)
    {
        if (filePath == null)
            return null;
        filePath = ExpandEnvironmentVariables(filePath);
        var fullPath = Path.GetFullPath(Path.Combine(config.ConfigurationBaseFolder, filePath));
        if (!isFolder && !File.Exists(fullPath))
            throw new DeveroomConfigurationException(
                $"Unable to access file '{fullPath}'. Please make sure you specify a path for an existing file for the {label} option.");
        if (isFolder && !Directory.Exists(fullPath))
            throw new DeveroomConfigurationException(
                $"Unable to access directory '{fullPath}'. Please make sure you specify a path for an existing directory for the {label} option.");
        return fullPath;
    }

    private string EnsureFullPath(DeveroomConfiguration config,
        Expression<Func<DeveroomConfiguration, string>> configAccessor, bool isFolder = false)
    {
        var filePath = configAccessor.Compile().Invoke(config);
        return EnsureFullPath(config, filePath, configAccessor.ToString(), isFolder);
    }
}
