using System.Configuration;
using System.IO;
using TechTalk.SpecFlow.Configuration;
using TechTalk.SpecFlow.Generator.Interfaces;

namespace Deveroom.VisualStudio.SpecFlowConnector.Generation.V2020
{
    public class SpecFlowV2020Generator : BaseGenerator
    {
        protected override SpecFlowConfigurationHolder CreateConfigHolder(string configFilePath)
        {
            var extension = Path.GetExtension(configFilePath) ?? "";
            var configFileContent = File.ReadAllText(configFilePath);
            switch (extension.ToLowerInvariant())
            {
                case ".config":
                {
                    return GetXmlConfigurationHolder(configFileContent);
                }
                case ".json":
                {
                    return new SpecFlowConfigurationHolder(ConfigSource.Json, configFileContent);
                }
            }
            throw new ConfigurationErrorsException($"Invalid config type: {configFilePath}");
        }
    }
}
