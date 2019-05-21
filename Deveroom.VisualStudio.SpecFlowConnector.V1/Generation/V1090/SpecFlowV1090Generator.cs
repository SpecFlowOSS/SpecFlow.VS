using System.IO;
using TechTalk.SpecFlow.Generator.Interfaces;

namespace Deveroom.VisualStudio.SpecFlowConnector.Generation.V1090
{
    public class SpecFlowV1090Generator : BaseGenerator
    {
        protected override SpecFlowConfigurationHolder CreateConfigHolder(string configFilePath)
        {
            var configFileContent = File.ReadAllText(configFilePath);
            return GetXmlConfigurationHolder(configFileContent);
        }
    }
}
