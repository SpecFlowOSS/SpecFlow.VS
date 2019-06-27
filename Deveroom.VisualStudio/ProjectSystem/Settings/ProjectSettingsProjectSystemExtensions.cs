namespace Deveroom.VisualStudio.ProjectSystem.Settings
{
    public static class ProjectSettingsProjectSystemExtensions
    {
        public static ProjectSettings GetProjectSettings(this IProjectScope projectScope)
        {
            var provider = GetProjectSettingsProvider(projectScope);
            return provider.GetProjectSettings();
        }

        public static ProjectSettingsProvider GetProjectSettingsProvider(this IProjectScope projectScope)
        {
            return projectScope.Properties.GetOrCreateSingletonProperty(() => new ProjectSettingsProvider(projectScope, new SpecFlowProjectSettingsProvider(projectScope)));
        }
    }
}
