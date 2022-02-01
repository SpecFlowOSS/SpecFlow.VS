#if NETFRAMEWORK
// ReSharper disable once CheckNamespace
namespace System.Runtime.Loader;

public abstract class AssemblyLoadContext
{
        // These methods load assemblies into the current AssemblyLoadContext
        // They may be used in the implementation of an AssemblyLoadContext derivation
        public Assembly LoadFromAssemblyPath(string assemblyPath)
        {
            if (assemblyPath == null)
            {
                throw new ArgumentNullException(nameof(assemblyPath));
            }

            return Assembly.LoadFrom(assemblyPath);
        }

        protected abstract Assembly Load(AssemblyName assemblyName);
}
#endif