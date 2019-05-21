using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace Deveroom.VisualStudio.SpecFlowConnector
{
    internal static class JsonSerialization
    {
        const string StartMarker = ">>>>>>>>>>";
        const string EndMarker = "<<<<<<<<<<";

        public static string StripResult(string content)
        {
            int startMarkerIndex = content.IndexOf(StartMarker, StringComparison.InvariantCultureIgnoreCase);
            if (startMarkerIndex >= 0)
                content = content.Substring(startMarkerIndex + StartMarker.Length);
            int endMarkerIndex = content.LastIndexOf(EndMarker, StringComparison.InvariantCultureIgnoreCase);
            if (endMarkerIndex >= 0)
                content = content.Substring(0, endMarkerIndex);
            return content.Trim();
        }

        public static string MarkResult(string content)
        {
            return StartMarker + Environment.NewLine + content + Environment.NewLine + EndMarker;
        }

        public static string SerializeObject(object obj)
        {
            return JsonConvert.SerializeObject(obj, GetJsonSerializerSettings(true));
        }

        public static T DeserializeObject<T>(string jsonString)
        {
            return JsonConvert.DeserializeObject<T>(jsonString, GetJsonSerializerSettings(true));
        }

        public static string SerializeObjectWithMarker(object obj)
        {
            return MarkResult(SerializeObject(obj));
        }

        public static T DeserializeObjectWithMarker<T>(string jsonString)
        {
            return DeserializeObject<T>(StripResult(jsonString));
        }

        public static JsonSerializerSettings GetJsonSerializerSettings(bool indented)
        {
            var serializerSettings = new JsonSerializerSettings();
            var contractResolver = new CamelCasePropertyNamesContractResolver();
            contractResolver.NamingStrategy.ProcessDictionaryKeys = false;
            serializerSettings.ContractResolver = contractResolver;
            serializerSettings.Converters = new List<JsonConverter> { new StringEnumConverter { CamelCaseText = true } };
            serializerSettings.Formatting = indented ? Formatting.Indented : Formatting.None;
            serializerSettings.NullValueHandling = NullValueHandling.Ignore;
            return serializerSettings;
        }

    }
}
