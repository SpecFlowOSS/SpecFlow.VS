﻿namespace SpecFlow.VisualStudio.VsxStubs;

public class StubEditorConfigOptionsProvider : IEditorConfigOptionsProvider
{
    public IEditorConfigOptions GetEditorConfigOptions(IWpfTextView textView) => new NullEditorConfigOptions();
}
