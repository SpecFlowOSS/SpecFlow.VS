﻿namespace SpecFlow.VisualStudio.Editor.Services.EditorConfig;

public interface IEditorConfigOptionsProvider
{
    IEditorConfigOptions GetEditorConfigOptions(IWpfTextView textView);
}
