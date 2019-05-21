using System;
using System.IO;
using System.Linq;
using Deveroom.VisualStudio.SpecFlowConnector.Discovery;
using FluentAssertions;
using TechTalk.SpecFlow.Configuration;
using Xunit;

namespace Deveroom.VisualStudio.SpecFlowConnector.V1.Tests
{
    public class SpecFlow21ConfigurationLoaderTests
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
            var sut = new SpecFlow21ConfigurationLoader(filePath);

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
            var sut = new SpecFlow21ConfigurationLoader(filePath, true);

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
            var sut = new SpecFlow21ConfigurationLoader(filePath, true);

            var config = sut.Load(_defaultConfig);

            config.Should().NotBeNull();
            config.FeatureLanguage.ToString().Should().Be("de-AT");
        }

        [Fact]
        public void Loads_input_config_for_null_config_file()
        {
            var sut = new SpecFlow21ConfigurationLoader(null);

            var config = sut.Load(_defaultConfig);

            config.Should().BeSameAs(_defaultConfig);
        }

        [Fact]
        public void Loads_default_config_for_null_config_file_and_null_input()
        {
            var sut = new SpecFlow21ConfigurationLoader(null);

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
            var sut = new SpecFlow21ConfigurationLoader(filePath);

            var config = sut.Load(_defaultConfig);

            config.Should().BeSameAs(_defaultConfig);
        }
    }
}
