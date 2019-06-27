using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Windows;
using System.Xml.Linq;
using System.Xml.XPath;
using Deveroom.VisualStudio.Annotations;
using Deveroom.VisualStudio.Configuration;
using Deveroom.VisualStudio.Diagonostics;
using Deveroom.VisualStudio.Monitoring;
using Newtonsoft.Json.Linq;

namespace Deveroom.VisualStudio.ProjectSystem.Configuration
{
    public class ProjectScopeDeveroomConfigurationProvider : IDeveroomConfigurationProvider, IDisposable
    {
        public const string SpecFlowJsonConfigFileName = "specflow.json";
        public const string SpecFlowAppConfigFileName = "App.config";

        private readonly IProjectScope _projectScope;
        private ConfigCache _configCache;
        private IDeveroomLogger Logger => _projectScope.IdeScope.Logger;
        private IMonitoringService MonitoringService => _projectScope.IdeScope.MonitoringService;
        private IFileSystem FileSystem => _projectScope.IdeScope.FileSystem;

        public event EventHandler<EventArgs> ConfigurationChanged;
        public event EventHandler<EventArgs> WeakConfigurationChanged
        {
            add => WeakEventManager<ProjectScopeDeveroomConfigurationProvider, EventArgs>.AddHandler(this, nameof(ConfigurationChanged), value);
            remove => WeakEventManager<ProjectScopeDeveroomConfigurationProvider, EventArgs>.RemoveHandler(this, nameof(ConfigurationChanged), value);
        }

        public ProjectScopeDeveroomConfigurationProvider([NotNull] IProjectScope projectScope)
        {
            _projectScope = projectScope ?? throw new ArgumentNullException(nameof(projectScope));
            InitializeConfiguration();

            _projectScope.IdeScope.WeakProjectsBuilt += ProjectSystemOnProjectsBuilt;
        }

        private void InitializeConfiguration()
        {
            var configSources = GetConfigSources().ToArray();
            _configCache = LoadConfiguration(configSources);
        }

        private void ProjectSystemOnProjectsBuilt(object sender, EventArgs e)
        {
            CheckConfiguration(true);
        }

        private void CheckConfiguration(bool triggerChanged)
        {
            Logger.LogVerbose("Checking configuration...");

            var configSources = GetConfigSources().ToArray();
            if (_configCache.ConfigSources.SequenceEqual(configSources))
                return; // no source changed

            var oldConfiguration = _configCache.Configuration;
            _configCache = LoadConfiguration(configSources);
            Logger.LogVerbose("Configuration loaded");

            if (triggerChanged && !oldConfiguration.Equals(_configCache.Configuration))
            {
                Logger.LogInfo("Configuration changed");
                ConfigurationChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public DeveroomConfiguration GetConfiguration()
        {
            return _configCache.Configuration;
        }

        private DeveroomConfiguration GetDefaultConfiguration()
        {
            return new DeveroomConfiguration();
        }

        private IEnumerable<ConfigSource> GetConfigSources()
        {
            var jsonSource = GetProjectConfigFilePath(SpecFlowJsonConfigFileName);
            if (jsonSource != null)
                yield return jsonSource;
            else
            {
                var appConfigSource = GetProjectConfigFilePath(SpecFlowAppConfigFileName);
                if (appConfigSource != null)
                    yield return appConfigSource;
            }

            var deveroomConfigSource = GetProjectConfigFilePath(DeveroomConfigurationLoader.DefaultConfigFileName);
            if (deveroomConfigSource != null)
                yield return deveroomConfigSource;
        }

        private ConfigSource GetProjectConfigFilePath(string fileName)
        {
            try
            {
                var projectFolder = _projectScope.ProjectFolder;
                var fileSystem = _projectScope.IdeScope.FileSystem;
                var configFilePath = fileSystem.GetFilePathIfExists(Path.Combine(projectFolder, fileName));
                if (configFilePath == null)
                    return null;
                return new ConfigSource(configFilePath, FileSystem.File.GetLastWriteTimeUtc(configFilePath));
            }
            catch (Exception ex)
            {
                Logger.LogDebugException(ex);
                return null;
            }
        }

        private ConfigCache LoadConfiguration(ConfigSource[] configSources)
        {
            var loadedSources = new List<ConfigSource>();
            var configuration = GetDefaultConfiguration();

            foreach (var configSource in configSources)
            {
                try
                {
                    var fileName = Path.GetFileName(configSource.FilePath);
                    if (SpecFlowAppConfigFileName.Equals(fileName, StringComparison.InvariantCultureIgnoreCase))
                    {
                        LoadFromSpecFlowXmlConfig(configSource.FilePath, configuration);
                    }

                    if (SpecFlowJsonConfigFileName.Equals(fileName, StringComparison.InvariantCultureIgnoreCase))
                    {
                        LoadFromSpecFlowJsonConfig(configSource.FilePath, configuration);
                    }

                    if (DeveroomConfigurationLoader.DefaultConfigFileName.Equals(fileName, StringComparison.InvariantCultureIgnoreCase))
                    {
                        LoadFromDeveroomConfig(configSource.FilePath, configuration);
                    }

                    loadedSources.Add(configSource);
                }
                catch (Exception ex)
                {
                    Logger.LogVerboseException(MonitoringService, ex, "Unable to load configuration");
                }
            }

            if (loadedSources.Any())
                configuration.ConfigurationChangeTime = loadedSources.Max(cs => cs.LastChangeTime);

            return new ConfigCache(configuration, loadedSources.ToArray());
        }

        private void LoadFromDeveroomConfig(string configSourceFilePath, DeveroomConfiguration configuration)
        {
            Logger.LogVerbose($"Loading Deveroom config from '{configSourceFilePath}'");
            var loader = new DeveroomConfigurationLoader();
            loader.Update(configSourceFilePath, configuration);
        }

        private string XPathEvaluateAttribute(XDocument doc, string xpath)
        {
            return (doc.XPathEvaluate(xpath) as IEnumerable)?.OfType<XAttribute>()?.FirstOrDefault()?.Value;
        }

        private void LoadFromSpecFlowXmlConfig(string configSourceFilePath, DeveroomConfiguration configuration)
        {
            var fileContent = FileSystem.File.ReadAllText(configSourceFilePath);
            var configDoc = XDocument.Parse(fileContent);
            var featureLang = XPathEvaluateAttribute(configDoc, "/configuration/specFlow/language/@feature");
            if (featureLang != null)
                configuration.DefaultFeatureLanguage = featureLang;
            var bindingCulture = XPathEvaluateAttribute(configDoc, "/configuration/specFlow/bindingCulture/@name");
            if (bindingCulture != null)
                configuration.ConfiguredBindingCulture = bindingCulture;
        }

        private void LoadFromSpecFlowJsonConfig(string configSourceFilePath, DeveroomConfiguration configuration)
        {
            var fileContent = FileSystem.File.ReadAllText(configSourceFilePath);
            var configDoc = JObject.Parse(fileContent);
            var featureLang = (string)configDoc["specFlow"]?["language"]?["feature"];
            if (featureLang != null)
                configuration.DefaultFeatureLanguage = featureLang;
            var bindingCulture = (string)configDoc["specFlow"]?["bindingCulture"]?["name"];
            if (bindingCulture != null)
                configuration.ConfiguredBindingCulture = bindingCulture;
        }

        public void Dispose()
        {
            _projectScope.IdeScope.WeakProjectsBuilt -= ProjectSystemOnProjectsBuilt;
        }
    }
}
