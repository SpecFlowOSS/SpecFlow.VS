using System;
using System.Configuration;
using System.IO;
using System.Xml;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TechTalk.SpecFlow.Configuration;
using TechTalk.SpecFlow.Configuration.AppConfig;
using TechTalk.SpecFlow.Configuration.JsonConfig;
using TechTalk.SpecFlow.Tracing;
using Formatting = Newtonsoft.Json.Formatting;

namespace Deveroom.VisualStudio.SpecFlowConnector.Discovery
{
    public class SpecFlow21ConfigurationLoader : IConfigurationLoader
    {
        private readonly string _configFilePath;
        private readonly bool _jsonSpecFlow2Mode;

        public SpecFlow21ConfigurationLoader(string configFilePath, bool jsonSpecFlow2Mode = false)
        {
            _configFilePath = configFilePath;
            _jsonSpecFlow2Mode = jsonSpecFlow2Mode;
        }

        public SpecFlowConfiguration Load(SpecFlowConfiguration specFlowConfiguration)
        {
            if (_configFilePath == null)
                return LoadDefaultConfiguration(specFlowConfiguration);

            var extension = Path.GetExtension(_configFilePath);
            var configFileContent = File.ReadAllText(_configFilePath);
            switch (extension.ToLowerInvariant())
            {
                case ".config":
                    {
                        var configDocument = new XmlDocument();
                        configDocument.LoadXml(configFileContent);
                        var specFlowNode = configDocument.SelectSingleNode("/configuration/specFlow");
                        if (specFlowNode == null)
                            return LoadDefaultConfiguration(specFlowConfiguration);

                        var configSection = ConfigurationSectionHandler.CreateFromXml(specFlowNode);
                        var loader = new AppConfigConfigurationLoader();
                        return loader.LoadAppConfig(specFlowConfiguration, configSection);
                    }
                case ".json":
                    {
                        if (_jsonSpecFlow2Mode)
                            configFileContent = ConvertToJsonSpecFlow2Style(configFileContent);

                        var loader = new JsonConfigurationLoader();
                        return loader.LoadJson(specFlowConfiguration, configFileContent);
                    }
            }
            throw new ConfigurationErrorsException($"Invalid config type: {_configFilePath}");
        }

        private string ConvertToJsonSpecFlow2Style(string configFileContent)
        {
            if (configFileContent.Contains("\"specFlow\""))
                return configFileContent;
            var content = JsonConvert.DeserializeObject(configFileContent);
            if (content is JObject contentJObj && contentJObj.TryGetValue("specFlow", out _))
                return configFileContent;

            var specFlow2StyleObject = new JObject(
                new JProperty("specFlow", content));
            return JsonConvert.SerializeObject(specFlow2StyleObject, Formatting.Indented);
        }

        private static SpecFlowConfiguration LoadDefaultConfiguration(SpecFlowConfiguration specFlowConfiguration)
        {
            return specFlowConfiguration ?? ConfigurationLoader.GetDefault();
        }

        public void TraceConfigSource(ITraceListener traceListener, SpecFlowConfiguration specFlowConfiguration)
        {
            traceListener.WriteToolOutput($"Using config from: {_configFilePath ?? "<default>"}");
        }

        public SpecFlowConfiguration Load(SpecFlowConfiguration specFlowConfiguration, ISpecFlowConfigurationHolder specFlowConfigurationHolder)
        {
            throw new NotSupportedException();
        }

        public SpecFlowConfiguration Update(SpecFlowConfiguration specFlowConfiguration, ConfigurationSectionHandler specFlowConfigSection)
        {
            throw new NotSupportedException();
        }
    }
}
