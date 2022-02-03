using System.Text.RegularExpressions;
using BoDi;
using TechTalk.SpecFlow;
using TechTalk.SpecFlow.Bindings;
using TechTalk.SpecFlow.Configuration;
using TechTalk.SpecFlow.Infrastructure;

namespace SpecFlowConnector.SpecFlowProxies;

public record StepScope(
    string Tag,
    string FeatureTitle,
    string ScenarioTitle
);

public record StepDefinition(
    string Type,
    string Regex//,
    //string Method,
    //string ParamTypes,
    //StepScope Scope,
    //string Expression,
    //string Error,
    //string SourceLocation
);

public record StepDefinitionBindingProxy(IStepDefinitionBinding StepDefinitionBinding)
{
    private readonly IStepDefinitionBinding _stepDefinitionBinding = StepDefinitionBinding;
    public string StepDefinitionType => _stepDefinitionBinding.StepDefinitionType.ToString();
    public Option<Regex> Regex => _stepDefinitionBinding.Regex;
}

public interface IBindingRegistryProxy
{
    IEnumerable<StepDefinitionBindingProxy> GetStepDefinitions(Assembly testAssembly, Option<FileDetails> configFile);
}

public abstract class BindingRegistryProxy<TGlobalContainer> : IBindingRegistryProxy
{
    public IEnumerable<StepDefinitionBindingProxy> GetStepDefinitions(Assembly testAssembly,
        Option<FileDetails> configFile)
    {
        var bindingRegistry = GetBindingRegistry(testAssembly, configFile);
        var stepDefinitionBindings = bindingRegistry.GetStepDefinitions();
        return stepDefinitionBindings
            .Select(ToProxy);
    }

    protected IBindingRegistry GetBindingRegistry(Assembly testAssembly, Option<FileDetails> configFile)
    {
        var globalContainer = CreateGlobalContainer(testAssembly, configFile);
        RegisterTypeAs<NoInvokeDependencyProvider.NullBindingInvoker, IBindingInvoker>(globalContainer);
        //RegisterTypeAs<FakeTestContext, TestContext>(globalContainer);
        CreateTestRunner(globalContainer, testAssembly);

        return ResolveBindingRegistry(testAssembly, globalContainer);
    }

    protected abstract void RegisterTypeAs<TType, TInterface>(TGlobalContainer globalContainer) where TType : class, TInterface;
    protected abstract TGlobalContainer CreateGlobalContainer(Assembly testAssembly, Option<FileDetails> configFile);
    protected abstract void CreateTestRunner(TGlobalContainer globalContainer, Assembly testAssembly);
    protected abstract IBindingRegistry ResolveBindingRegistry(Assembly testAssembly, TGlobalContainer globalContainer);
    protected abstract StepDefinitionBindingProxy ToProxy(IStepDefinitionBinding sd);
}

public class BindingRegistryProxyV3_9_22 : BindingRegistryProxy<IObjectContainer>
{
    private readonly IFileSystem _fileSystem;

    public BindingRegistryProxyV3_9_22(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }

    protected override void RegisterTypeAs<TType, TInterface>(IObjectContainer globalContainer) 
        => globalContainer.RegisterTypeAs<TType, TInterface>();

    protected override IObjectContainer CreateGlobalContainer(Assembly testAssembly, Option<FileDetails> configFile)
    {
        var containerBuilder = new ContainerBuilder(new NoInvokeDependencyProvider());
        IConfigurationLoader configurationLoader = new SpecFlowConfigurationLoader(configFile, _fileSystem);
        var configurationProvider = new DefaultRuntimeConfigurationProvider(configurationLoader);
        return containerBuilder.CreateGlobalContainer(testAssembly, configurationProvider);
    }

    protected override void CreateTestRunner(IObjectContainer globalContainer, Assembly testAssembly)
    {
        var testRunnerManager = (TestRunnerManager)globalContainer.Resolve<ITestRunnerManager>();

        testRunnerManager.Initialize(testAssembly);
        testRunnerManager.CreateTestRunner(0);
    }

    protected override IBindingRegistry ResolveBindingRegistry(Assembly testAssembly, IObjectContainer globalContainer)
        => globalContainer.Resolve<IBindingRegistry>();

    protected override StepDefinitionBindingProxy ToProxy(IStepDefinitionBinding sd) => new StepDefinitionBindingProxy(sd);
}

public interface IRuntimeConfigurationProviderProxy
{
}

public class RuntimeConfigurationProviderProxyProxy : IRuntimeConfigurationProviderProxy
{
}
