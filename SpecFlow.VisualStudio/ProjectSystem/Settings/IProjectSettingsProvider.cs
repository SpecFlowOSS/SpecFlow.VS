#nullable enable

namespace SpecFlow.VisualStudio.ProjectSystem.Settings;

public interface IProjectSettingsProvider
{
    event EventHandler<EventArgs> WeakSettingsInitialized;
    event EventHandler<EventArgs> SettingsInitialized;

    ProjectSettings GetProjectSettings();
    ProjectSettings CheckProjectSettings();
}
