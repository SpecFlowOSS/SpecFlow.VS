using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SpecFlowConnector.SpecFlowProxies;

public class SpecFlow21ConfigurationLoader : SpecFlowConfigurationLoader
{
    public SpecFlow21ConfigurationLoader(Option<FileDetails> configFile, IFileSystem fileSystem) : base(configFile, fileSystem)
    {
    }

    protected override string ConvertToJsonSpecFlow3Style(string configFileContent)
    {
        var content = JsonConvert.DeserializeObject<JObject>(configFileContent);

        if (!content.TryGetValue("specFlow", out var specFlowObject))
            return configFileContent;

        var configObject = new JObject(specFlowObject.First);

        var modifiedContent = JsonConvert.SerializeObject(configObject, Formatting.Indented);
        return modifiedContent;
    }
}
