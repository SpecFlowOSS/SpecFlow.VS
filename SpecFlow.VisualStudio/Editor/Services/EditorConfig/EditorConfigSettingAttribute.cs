using System;

namespace SpecFlow.VisualStudio.Editor.Services.EditorConfig;

[AttributeUsage(AttributeTargets.Property)]
public class EditorConfigSettingAttribute : Attribute
{
    public EditorConfigSettingAttribute(string editorConfigSettingName)
    {
        EditorConfigSettingName = editorConfigSettingName;
    }

    public string EditorConfigSettingName { get; }
}
