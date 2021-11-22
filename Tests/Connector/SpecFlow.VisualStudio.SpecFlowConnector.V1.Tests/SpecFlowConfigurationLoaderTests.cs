using System.IO;
using FluentAssertions;
using SpecFlow.VisualStudio.SpecFlowConnector.Discovery;
using TechTalk.SpecFlow.Configuration;
using Xunit;

namespace SpecFlow.VisualStudio.SpecFlowConnector.V1.Tests
{
    public class SpecFlowConfigurationLoaderTests
    {
        private readonly SpecFlowConfiguration _defaultConfig = ConfigurationLoader.GetDefault();

        [Fact]
        public void Loads_config_from_AppConfig()
        {
            var configFileContent = @"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <configSections>
    <section name=""specFlow"" type=""TechTalk.SpecFlow.Configuration.ConfigurationSectionHandler, TechTalk.SpecFlow"" />
  </configSections>
  <specFlow>
    <language feature=""de-AT"" />
  </specFlow>
</configuration>";
            var filePath = Path.GetTempPath() + ".config";
            File.WriteAllText(filePath, configFileContent);
            var sut = new SpecFlowConfigurationLoader(filePath);

            var config = sut.Load(_defaultConfig);

            config.Should().NotBeNull();
            config.FeatureLanguage.ToString().Should().Be("de-AT");
        }

        [Fact]
        public void Loads_config_from_JSON_SpecFlow2_Style()
        {
            var configFileContent = @"
{
    ""specFlow"": {
        ""language"": {
            ""feature"": ""de-AT""
        }
    }
}";
            var filePath = Path.GetTempPath() + ".json";
            File.WriteAllText(filePath, configFileContent);
            var sut = new SpecFlow21ConfigurationLoader(filePath);

            var config = sut.Load(_defaultConfig);

            config.Should().NotBeNull();
            config.FeatureLanguage.ToString().Should().Be("de-AT");
        }

        [Fact]
        public void Loads_config_from_JSON_SpecFlow3_Style()
        {
            var configFileContent = @"
{
    ""language"": {
        ""feature"": ""de-AT""
    }
}";
            var filePath = Path.GetTempPath() + ".json";
            File.WriteAllText(filePath, configFileContent);
            var sut = new SpecFlowConfigurationLoader(filePath);

            var config = sut.Load(_defaultConfig);

            config.Should().NotBeNull();
            config.FeatureLanguage.ToString().Should().Be("de-AT");
        }

        [Fact]
        public void Loads_input_config_for_null_config_file()
        {
            var sut = new SpecFlowConfigurationLoader(null);

            var config = sut.Load(_defaultConfig);

            config.Should().BeSameAs(_defaultConfig);
        }

        [Fact]
        public void Loads_default_config_for_null_config_file_and_null_input()
        {
            var sut = new SpecFlowConfigurationLoader(null);

            var config = sut.Load(null);

            config.Should().NotBeNull();
            config.FeatureLanguage.ToString().Should().Be("en-US");
        }


        [Fact]
        public void Loads_input_config_for_AppConfig_file_without_specflow_node()
        {
            var configFileContent = @"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
</configuration>";
            var filePath = Path.GetTempPath() + ".config";
            File.WriteAllText(filePath, configFileContent);
            var sut = new SpecFlowConfigurationLoader(filePath);

            var config = sut.Load(_defaultConfig);

            config.Should().BeSameAs(_defaultConfig);
        }
    }

}
