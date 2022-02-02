using BoDi;
using TechTalk.SpecFlow.Bindings;
using TechTalk.SpecFlow.Configuration;
using TechTalk.SpecFlow.Infrastructure;

namespace SpecFlowConnector.SpecFlowProxies;

public interface IBindingRegistryProxy
{
    IEnumerable<IStepDefinitionBindingProxy> GetStepDefinitions(Assembly testAssembly, Option<FileDetails> configFile);
}

public abstract class BindingRegistryProxy<TGlobalContainer> : IBindingRegistryProxy
{
    public IEnumerable<IStepDefinitionBindingProxy> GetStepDefinitions(Assembly testAssembly,
        Option<FileDetails> configFile)
    {
        var bindingRegistry = GetBindingRegistry(testAssembly, configFile);
        return Array.Empty<IStepDefinitionBindingProxy>();
    }

    protected IBindingRegistry GetBindingRegistry(Assembly testAssembly, Option<FileDetails> configFile)
    {
        var globalContainer = CreateGlobalContainer(testAssembly, configFile);
        //RegisterTypeAs<NoInvokeDependencyProvider.NullBindingInvoker, IBindingInvoker>(globalContainer);
        //RegisterTypeAs<FakeTestContext, TestContext>(globalContainer);
        return ResolveBindingRegistry(testAssembly, globalContainer);
    }

    protected abstract TGlobalContainer CreateGlobalContainer(Assembly testAssembly, Option<FileDetails> configFile);
    protected abstract IBindingRegistry ResolveBindingRegistry(Assembly testAssembly, TGlobalContainer globalContainer);
}

public class BindingRegistryProxyV3_9_22 : BindingRegistryProxy<IObjectContainer>
{
    private readonly IFileSystem _fileSystem;

    public BindingRegistryProxyV3_9_22(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }

    protected override IObjectContainer CreateGlobalContainer(Assembly testAssembly, Option<FileDetails> configFile)
    {
        var containerBuilder = new ContainerBuilder(new NoInvokeDependencyProvider());
        IConfigurationLoader configurationLoader = new SpecFlowConfigurationLoader(configFile, _fileSystem);
        var configurationProvider = new DefaultRuntimeConfigurationProvider(configurationLoader);
        return containerBuilder.CreateGlobalContainer(testAssembly, configurationProvider);
    }

    protected override IBindingRegistry ResolveBindingRegistry(Assembly testAssembly, IObjectContainer globalContainer)
        => globalContainer.Resolve<IBindingRegistry>();
}

public interface IStepDefinitionBindingProxy
{
}

public interface IRuntimeConfigurationProviderProxy
{
}

public class RuntimeConfigurationProviderProxyProxy : IRuntimeConfigurationProviderProxy
{
}
