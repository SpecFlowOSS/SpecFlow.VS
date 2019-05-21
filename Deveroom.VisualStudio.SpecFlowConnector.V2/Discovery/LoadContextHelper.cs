using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using McMaster.NETCore.Plugins;
using McMaster.NETCore.Plugins.LibraryModel;
using McMaster.NETCore.Plugins.Loader;
using Microsoft.Extensions.DependencyModel;

namespace Deveroom.VisualStudio.SpecFlowConnector.Discovery
{
    internal static class LoadContextHelper
    {
        public static AssemblyLoadContext CreateLoadContext(string assemblyFile)
        {
            var baseDir = Path.GetDirectoryName(assemblyFile);
            var assemblyName = new AssemblyName(Path.GetFileNameWithoutExtension(assemblyFile));
            return CreateLoadContext(baseDir, assemblyName);
        }

        /// <summary>
        /// Based on https://github.com/natemcmaster/DotNetCorePlugins/blob/master/src/Plugins/PluginLoader.cs
        /// </summary>
        public static AssemblyLoadContext CreateLoadContext(string baseDir,
            AssemblyName mainAssemblyName,
            PluginLoaderOptions loaderOptions = PluginLoaderOptions.None,
            IEnumerable<AssemblyName> preferDefaultLoadContextForAssemblies = null,
            IEnumerable<AssemblyName> privateAssemblies = null)
        {
            var depsJsonFile = Path.Combine(baseDir, mainAssemblyName.Name + ".deps.json");

            var builder = new AssemblyLoadContextBuilder();

            if (File.Exists(depsJsonFile))
            {
                builder.AddDependencyContextWithCompileDeps(depsJsonFile);
            }

            builder.SetBaseDirectory(baseDir);

            if (privateAssemblies != null)
                foreach (var ext in privateAssemblies)
                {
                    builder.PreferLoadContextAssembly(ext);
                }

            if (loaderOptions.HasFlag(PluginLoaderOptions.PreferSharedTypes))
            {
                builder.PreferDefaultLoadContext(true);
            }

            if (preferDefaultLoadContextForAssemblies != null)
            {
                foreach (var assemblyName in preferDefaultLoadContextForAssemblies)
                {
                    builder.PreferDefaultLoadContextAssembly(assemblyName);
                }
            }

            var pluginRuntimeConfigFile = Path.Combine(baseDir, mainAssemblyName.Name + ".runtimeconfig.json");

            builder.TryAddAdditionalProbingPathFromRuntimeConfig(pluginRuntimeConfigFile, true, out _);

            foreach (var runtimeconfig in Directory.GetFiles(AppContext.BaseDirectory, "*.runtimeconfig.json"))
            {
                builder.TryAddAdditionalProbingPathFromRuntimeConfig(runtimeconfig, true, out _);
            }

            return builder.Build();
        }

        public static AssemblyLoadContextBuilder AddDependencyContextWithCompileDeps(this AssemblyLoadContextBuilder builder, string depsFilePath)
        {
            var reader = new DependencyContextJsonReader();
            using (var file = File.OpenRead(depsFilePath))
            {
                var deps = reader.Read(file);
                builder.SetBaseDirectory(Path.GetDirectoryName(depsFilePath));
                builder.AddDependencyContext(deps);
                builder.AddCompileDependencies(deps);
            }
            return builder;
        }

        public static void AddCompileDependencies(this AssemblyLoadContextBuilder builder, DependencyContext dependencyContext)
        {
            foreach (var library in dependencyContext.CompileLibraries.Where(cl => !dependencyContext.RuntimeLibraries.Any(rl => cl.Name.Equals(rl.Name))))
            {
                foreach (var libraryAssembly in library.Assemblies.Where(a => a.StartsWith("lib", StringComparison.OrdinalIgnoreCase)))
                {
                    var managedLibrary = ManagedLibrary.CreateFromPackage(library.Name, library.Version, libraryAssembly);
                    try
                    {
                        builder.AddManagedLibrary(managedLibrary);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }
            }
        }
    }
}
