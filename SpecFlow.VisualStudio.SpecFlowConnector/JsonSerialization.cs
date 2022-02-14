using System.Text.Json;
using System.Text.Json.Serialization;

namespace SpecFlowConnector;

public static class JsonSerialization
{
    private const string StartMarker = ">>>>>>>>>>";
    private const string EndMarker = "<<<<<<<<<<";

    public static string MarkResult(string content) =>
        StartMarker + Environment.NewLine + content + Environment.NewLine + EndMarker;

    public static string SerializeObject(object obj)
    {
        return JsonSerializer.Serialize(obj, GetJsonSerializerSettings());
    }

    public static Option<TResult> DeserializeObject<TResult>(string json)
    {
        var deserializeObject = JsonSerializer.Deserialize<TResult>(json, GetJsonSerializerSettings());
        return deserializeObject;
    }

    public static JsonSerializerOptions GetJsonSerializerSettings()
    {
        var serializerSettings = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault
        };
//        var contractResolver = new CamelCasePropertyNamesContractResolver();
//        contractResolver.NamingStrategy.ProcessDictionaryKeys = false;
//        serializerSettings.ContractResolver = contractResolver;
//        serializerSettings.Converters = new List<JsonConverter>
//        {
//            new StringEnumConverter
//            {
//#if OLD_JSONNET_API
//                CamelCaseText = true
//#else
//                //NamingStrategy = new CamelCaseNamingStrategy()
//#endif
//            }
//        };
//        serializerSettings.Formatting = indented ? Formatting.Indented : Formatting.None;
//        serializerSettings.NullValueHandling = NullValueHandling.Ignore;
        return serializerSettings;
    }
}

