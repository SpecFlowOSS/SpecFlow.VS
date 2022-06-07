using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SpecFlowConnector;

public static class JsonSerialization
{
    private const string StartMarker = ">>>>>>>>>>";
    private const string EndMarker = "<<<<<<<<<<";

    public static string MarkResult(string content) =>
        StartMarker + Environment.NewLine + content + Environment.NewLine + EndMarker;

    public static string SerializeObject(object obj) => JsonSerializer.Serialize(obj, JsonSerializerSettings);

    public static Option<TResult> DeserializeObject<TResult>(string json)
    {
        try
        {
            var deserializeObject = JsonSerializer.Deserialize<TResult>(json, JsonSerializerSettings);
            return deserializeObject;
        }
        catch (Exception)
        {
            return None.Value;
        }
    }

    public static JsonSerializerOptions JsonSerializerSettings = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
}
