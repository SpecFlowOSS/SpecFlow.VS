namespace SpecFlow.VisualStudio.Editor.Services.EditorConfig
{
    internal class NullEditorConfigOptions : IEditorConfigOptions
    {
        public static readonly NullEditorConfigOptions Instance = new();

        public bool GetBoolOption(string editorConfigKey, bool defaultValue)
            => defaultValue;

        public TResult GetOption<TResult>(string editorConfigKey, TResult defaultValue)
            => default;
    }
}