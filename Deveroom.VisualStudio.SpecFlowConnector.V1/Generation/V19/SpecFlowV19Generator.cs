using System.IO;
using TechTalk.SpecFlow.Generator.Interfaces;

namespace Deveroom.VisualStudio.SpecFlowConnector.Generation.V19
{
    public class SpecFlowV19Generator : BaseGenerator
    {
        protected override SpecFlowConfigurationHolder CreateConfigHolder(string configFilePath)
        {
            var configFileContent = File.ReadAllText(configFilePath);
            return GetXmlConfigurationHolder(configFileContent);
        }
    }
}
