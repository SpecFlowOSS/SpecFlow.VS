using TechTalk.SpecFlow.Bindings;

namespace SpecFlowConnector.SpecFlowProxies;

public abstract class BindingRegistryFactory<TGlobalContainer> : IBindingRegistryFactory
{
    public IBindingRegistry GetBindingRegistry(AssemblyLoadContext assemblyLoadContext,
        Assembly testAssembly, Option<FileDetails> configFile)
    {
        var globalContainer = CreateGlobalContainer(assemblyLoadContext, configFile, testAssembly);
        RegisterTypeAs<NoInvokeDependencyProvider.NullBindingInvoker, IBindingInvoker>(globalContainer);
        //RegisterTypeAs<FakeTestContext, TestContext>(globalContainer);
        CreateTestRunner(globalContainer, testAssembly);

        return ResolveBindingRegistry(testAssembly, globalContainer);
    }

    protected abstract void RegisterTypeAs<TType, TInterface>(TGlobalContainer globalContainer) where TType : class, TInterface;
    protected abstract TGlobalContainer CreateGlobalContainer(AssemblyLoadContext assemblyLoadContext,
        Option<FileDetails> configFile, Assembly testAssembly);
    protected abstract void CreateTestRunner(TGlobalContainer globalContainer, Assembly testAssembly);
    protected abstract IBindingRegistry ResolveBindingRegistry(Assembly testAssembly, TGlobalContainer globalContainer);
}
