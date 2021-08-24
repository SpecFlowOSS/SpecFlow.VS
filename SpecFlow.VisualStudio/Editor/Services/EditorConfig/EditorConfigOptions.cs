using System.Reflection;
using Microsoft.CodeAnalysis.Options;

namespace SpecFlow.VisualStudio.Editor.Services.EditorConfig
{
    public class EditorConfigOptions : IEditorConfigOptions
    {
        private readonly DocumentOptionSet _options;

        public EditorConfigOptions(DocumentOptionSet options)
        {
            _options = options;
        }

        public bool GetBoolOption(string editorConfigKey, bool defaultValue)
        {
            var storageLocation = CreateBoolStorageLocation(editorConfigKey);
            if (storageLocation == null)
                return defaultValue;
            return _options.GetOption(new Option<bool>("specflow.vs", editorConfigKey, defaultValue, storageLocation));
        }

        private OptionStorageLocation CreateBoolStorageLocation(string editorConfigKey)
        {
            return
                typeof(OptionSet).Assembly.GetType("Microsoft.CodeAnalysis.Options.EditorConfigStorageLocation", false)?
                        .GetMethod("ForBoolOption", BindingFlags.Public | BindingFlags.Static)?
                        .Invoke(null, new object[] { editorConfigKey })
                    as OptionStorageLocation;
        }
    }
}