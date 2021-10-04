using System;
using SpecFlow.VisualStudio.ProjectSystem.Settings;

namespace SpecFlow.VisualStudio.Discovery
{
    internal sealed class ProjectBindingRegistryCacheUninitialized : ProjectBindingRegistryCache
    {
        public ProjectBindingRegistryCacheUninitialized() : base(ProjectBindingRegistry.Invalid)
        {
        }
    }
    internal sealed class ProjectBindingRegistryCacheUninitializedProjectSettings : ProjectBindingRegistryCache
    {
        public ProjectBindingRegistryCacheUninitializedProjectSettings() : base(ProjectBindingRegistry.Invalid)
        {
        }
    }

    internal sealed class ProjectBindingRegistryCacheTestAssemblyNotFound : ProjectBindingRegistryCache
    {
        public ProjectBindingRegistryCacheTestAssemblyNotFound() : base(ProjectBindingRegistry.Invalid)
        {
        }
    }

    internal sealed class ProjectBindingRegistryCacheError : ProjectBindingRegistryCache
    {
        public ProjectBindingRegistryCacheError() : base(ProjectBindingRegistry.Invalid)
        {
        }
    }

    internal sealed class ProjectBindingRegistryCacheDiscovered : ProjectBindingRegistryCache
    {
        public ProjectBindingRegistryCacheDiscovered(ProjectBindingRegistry bindingRegistry, ProjectSettings projectSettings, DateTimeOffset lastChangeTime) 
        : base(bindingRegistry)
        {
            ProjectSettings = projectSettings;
            TestAssemblyWriteTime = lastChangeTime;
        }

        public ProjectSettings ProjectSettings { get; }
        public DateTimeOffset TestAssemblyWriteTime { get; }

        public override ProjectBindingRegistryCache WithBindingRegistry(ProjectBindingRegistry bindingRegistry)
        {
            return new ProjectBindingRegistryCacheDiscovered(bindingRegistry, ProjectSettings, TestAssemblyWriteTime);
        }

        public override bool IsUpToDate(ProjectSettings projectSettings, DateTimeOffset lastChangeTime)
        {
            return Equals(ProjectSettings, projectSettings) && TestAssemblyWriteTime == lastChangeTime;
        }

        public override bool IsDiscovered => true;
    }

    internal sealed class ProjectBindingRegistryCacheNonSpecFlowTestProject : ProjectBindingRegistryCache
    {
        public ProjectBindingRegistryCacheNonSpecFlowTestProject() : base(ProjectBindingRegistry.Invalid)
        {
        }
    }

    internal abstract class ProjectBindingRegistryCache
    {
        public ProjectBindingRegistry BindingRegistry { get; }

        protected ProjectBindingRegistryCache(ProjectBindingRegistry bindingRegistry)
        {
            BindingRegistry = bindingRegistry;
        }

        public virtual ProjectBindingRegistryCache WithBindingRegistry(ProjectBindingRegistry bindingRegistry) => this;

        public virtual bool IsUpToDate(ProjectSettings projectSettings, DateTimeOffset lastChangeTime) => false;

        public virtual bool IsDiscovered => false;
    }
}