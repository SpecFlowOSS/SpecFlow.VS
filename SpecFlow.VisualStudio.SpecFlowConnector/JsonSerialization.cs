using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace SpecFlowConnector;

internal static class JsonSerialization
{
    private const string StartMarker = ">>>>>>>>>>";
    private const string EndMarker = "<<<<<<<<<<";

    public static string MarkResult(string content) =>
        StartMarker + Environment.NewLine + content + Environment.NewLine + EndMarker;

    public static string SerializeObject(object obj) =>
        JsonConvert.SerializeObject(obj, GetJsonSerializerSettings(true));

    public static JsonSerializerSettings GetJsonSerializerSettings(bool indented)
    {
        var serializerSettings = new JsonSerializerSettings();
        var contractResolver = new CamelCasePropertyNamesContractResolver();
        contractResolver.NamingStrategy.ProcessDictionaryKeys = false;
        serializerSettings.ContractResolver = contractResolver;
        serializerSettings.Converters = new List<JsonConverter>
        {
            new StringEnumConverter
            {
#if OLD_JSONNET_API
                CamelCaseText = true
#else
                //NamingStrategy = new CamelCaseNamingStrategy()
#endif
            }
        };
        serializerSettings.Formatting = indented ? Formatting.Indented : Formatting.None;
        serializerSettings.NullValueHandling = NullValueHandling.Ignore;
        return serializerSettings;
    }
}

