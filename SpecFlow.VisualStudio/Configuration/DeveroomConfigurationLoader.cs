using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using SpecFlow.VisualStudio.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace SpecFlow.VisualStudio.Configuration
{
    internal interface IConfigDeserializer
    {
        DeveroomConfiguration Deserialize(string jsonString);
        void Populate(string jsonString, DeveroomConfiguration config);
    }

    internal class JsonNetConfigDeserializer : IConfigDeserializer
    {
        public DeveroomConfiguration Deserialize(string jsonString)
        {
            return JsonConvert.DeserializeObject<DeveroomConfiguration>(jsonString, GetJsonSerializerSettings(true));
        }

        public void Populate(string jsonString, DeveroomConfiguration config)
        {
            JsonConvert.PopulateObject(jsonString, config, GetJsonSerializerSettings(true));
        }

        public static JsonSerializerSettings GetJsonSerializerSettings(bool indented)
        {
            var serializerSettings = new JsonSerializerSettings();
            var contractResolver = new CamelCasePropertyNamesContractResolver();
            contractResolver.NamingStrategy.ProcessDictionaryKeys = false;
            serializerSettings.ContractResolver = contractResolver;
            serializerSettings.Converters = new List<JsonConverter> { new StringEnumConverter
            {
#if OLD_JSONNET_API
                CamelCaseText = true
#else
                NamingStrategy = new CamelCaseNamingStrategy()
#endif
            } };
            serializerSettings.Formatting = indented ? Formatting.Indented : Formatting.None;
            serializerSettings.NullValueHandling = NullValueHandling.Ignore;
            return serializerSettings;
        }
    }

    public class DeveroomConfigurationLoader
    {
        private readonly IConfigDeserializer _configDeserializer;
        public const string DefaultConfigFileName = "deveroom.json";

        public DeveroomConfigurationLoader() : this(new JsonNetConfigDeserializer()) { }
        internal DeveroomConfigurationLoader(IConfigDeserializer configDeserializer)
        {
            _configDeserializer = configDeserializer;
        }

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
                throw new DeveroomConfigurationException($"The specified config file '{configFilePath}' does not contain a folder.");

            var jsonString = File.ReadAllText(configFilePath);
            Update(config, jsonString, configFolder);
        }

        public void Update(DeveroomConfiguration config, string configFileContent, string configFolder)
        {
            _configDeserializer.Populate(configFileContent, config);

            config.ConfigurationBaseFolder = configFolder;

            config.SpecFlow.ConfigFilePath = EnsureFullPath(config, c => c.SpecFlow.ConfigFilePath);
            config.SpecFlow.GeneratorFolder = EnsureFullPath(config, c => c.SpecFlow.GeneratorFolder, isFolder: true);
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
                throw new DeveroomConfigurationException($"Unable to access file '{fullPath}'. Please make sure you specify a path for an existing file for the {label} option.");
            if (isFolder && !Directory.Exists(fullPath))
                throw new DeveroomConfigurationException($"Unable to access directory '{fullPath}'. Please make sure you specify a path for an existing directory for the {label} option.");
            return fullPath;
        }

        private string EnsureFullPath(DeveroomConfiguration config, Expression<Func<DeveroomConfiguration, string>> configAccessor, bool isFolder = false)
        {
            var filePath = configAccessor.Compile().Invoke(config);
            return EnsureFullPath(config, filePath, configAccessor.ToString(), isFolder);
        }
    }
}
