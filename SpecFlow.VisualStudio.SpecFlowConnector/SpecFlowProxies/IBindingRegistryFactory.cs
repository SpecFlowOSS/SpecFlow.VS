using SpecFlow.VisualStudio.SpecFlowConnector.Models;
using TechTalk.SpecFlow.Bindings;

namespace SpecFlowConnector.SpecFlowProxies;

public interface IBindingRegistryFactory
{
    IBindingRegistry GetBindingRegistry(AssemblyLoadContext assemblyLoadContext,
        Assembly testAssembly, Option<FileDetails> configFile);
}