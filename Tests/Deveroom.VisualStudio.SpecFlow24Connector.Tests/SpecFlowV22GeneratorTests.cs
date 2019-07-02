using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Deveroom.VisualStudio.SpecFlowConnector.Generation.V22;
using FluentAssertions;
using TechTalk.SpecFlow.Generator.Interfaces;
using Xunit;

namespace Deveroom.VisualStudio.SpecFlow24Connector.Tests
{
    public class SpecFlowV22GeneratorTests
    {
        class TestableSpecFlowV22Generator : SpecFlowV22Generator
        {
            public ITestGenerator TestableCreateGenerator(string projectFolder, string configFilePath, string targetExtension, string projectDefaultNamespace)
            {
                return base.CreateGenerator(projectFolder, configFilePath, targetExtension, projectDefaultNamespace);
            }
        }

        [Fact]
        public void Can_create_generator_with_JSON_config_file()
        {
            var sut = new TestableSpecFlowV22Generator();

            var configFilePath = Path.Combine(Path.GetTempPath(), GetType().Name + ".specflow.v2.json");
            File.WriteAllText(configFilePath, @"{
    ""specFlow"": {}
}");

            var result = sut.TestableCreateGenerator(Path.GetDirectoryName(configFilePath), configFilePath, ".cs", null);

            result.Should().NotBeNull();
        }

        [Fact]
        public void Tolerates_V3_style_JSON_config_file()
        {
            var sut = new TestableSpecFlowV22Generator();

            var configFilePath = Path.Combine(Path.GetTempPath(), GetType().Name + ".specflow.v3.json");
            File.WriteAllText(configFilePath, @"{}");

            var result = sut.TestableCreateGenerator(Path.GetDirectoryName(configFilePath), configFilePath, ".cs", null);

            result.Should().NotBeNull();
        }
    }
}
