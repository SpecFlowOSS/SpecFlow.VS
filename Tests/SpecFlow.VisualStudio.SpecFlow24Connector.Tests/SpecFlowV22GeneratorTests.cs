﻿using System;
using System.IO;
using System.Linq;
using FluentAssertions;
using SpecFlow.VisualStudio.SpecFlowConnector.Generation.V22;
using TechTalk.SpecFlow.Generator.Interfaces;
using Xunit;

namespace SpecFlow.VisualStudio.SpecFlow24Connector.Tests
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