using SpecFlow.VisualStudio.Generation;
using SpecFlow.VisualStudio.Snippets;

namespace SpecFlow.VisualStudio.ProjectSystem;

public static class ProjectScopeServicesExtensions
{
    public static void InitializeServices(this IProjectScope projectScope)
    {
        projectScope.GetDeveroomConfigurationProvider();
        projectScope.GetProjectSettingsProvider();
    }

    public static IDiscoveryService GetDiscoveryService(this IProjectScope projectScope)
    {
        return projectScope.Properties.GetOrCreateSingletonProperty(() =>
        {
            var discoveryResultProvider = new DiscoveryResultProvider(projectScope);
            var bindingRegistryCache = new ProjectBindingRegistryCache(projectScope.IdeScope);
            IDiscoveryService discoveryService =
                new DiscoveryService(projectScope, discoveryResultProvider, bindingRegistryCache);
            discoveryService.TriggerDiscovery("ProjectScopeServicesExtensions.GetDiscoveryService");
            return discoveryService;
        });
    }

    public static GenerationService GetGenerationService(this IProjectScope projectScope)
    {
        if (!projectScope.GetProjectSettings().IsSpecFlowProject)
            return null;

        return projectScope.Properties.GetOrCreateSingletonProperty(() =>
            new GenerationService(projectScope));
    }

    public static SnippetService GetSnippetService(this IProjectScope projectScope)
    {
        if (!projectScope.GetProjectSettings().IsSpecFlowProject)
            return null;

        return projectScope.Properties.GetOrCreateSingletonProperty(() =>
            new SnippetService(projectScope));
    }
}
