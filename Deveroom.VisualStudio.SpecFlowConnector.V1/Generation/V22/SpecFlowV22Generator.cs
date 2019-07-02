using System;
using System.Configuration;
using System.IO;
using Newtonsoft.Json.Linq;
using TechTalk.SpecFlow.Configuration;
using TechTalk.SpecFlow.Generator.Interfaces;

namespace Deveroom.VisualStudio.SpecFlowConnector.Generation.V22
{
    public class SpecFlowV22Generator : BaseGenerator
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
                    if (!IsSpecFlowV2Json(configFileContent))
                        return new SpecFlowConfigurationHolder();

                    return new SpecFlowConfigurationHolder(ConfigSource.Json, configFileContent);
                }
            }
            throw new ConfigurationErrorsException($"Invalid config type: {configFilePath}");
        }

        private bool IsSpecFlowV2Json(string configFileContent)
        {
            try
            {
                var configObject = JObject.Parse(configFileContent);
                return configObject["specFlow"] != null;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
