using System;

namespace SpecFlow.VisualStudio.Editor.Services.EditorConfig
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class EditorConfigSettingAttribute : Attribute
    {
        public string EditorConfigSettingName { get; }

        public EditorConfigSettingAttribute(string editorConfigSettingName)
        {
            EditorConfigSettingName = editorConfigSettingName;
        }
    }
}