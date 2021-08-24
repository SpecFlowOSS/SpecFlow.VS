namespace SpecFlow.VisualStudio.Editor.Services.EditorConfig
{
    public interface IEditorConfigOptions
    {
        bool GetBoolOption(string editorConfigKey, bool defaultValue);
    }
}