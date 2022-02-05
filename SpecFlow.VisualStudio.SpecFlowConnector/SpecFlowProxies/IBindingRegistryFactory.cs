﻿using System.Text.RegularExpressions;
using BoDi;
using SpecFlow.VisualStudio.SpecFlowConnector.Models;
using TechTalk.SpecFlow;
using TechTalk.SpecFlow.Bindings;
using TechTalk.SpecFlow.Bindings.Reflection;
using TechTalk.SpecFlow.Configuration;
using TechTalk.SpecFlow.Infrastructure;

namespace SpecFlowConnector.SpecFlowProxies;

public record StepScope(
    string? Tag,
    string FeatureTitle,
    string ScenarioTitle
);

public record StepDefinition(
    string Type,
    string? Regex,
    string Method,
    string? ParamTypes,
    StepScope? Scope,
    string? Expression,
    string? Error,
    string SourceLocation
);

public record BindingMethodAdapter(IBindingMethod Adaptee)
{
    public override string ToString() => Adaptee.ToString();
    public IEnumerable<string> ParameterTypeNames => Adaptee.Parameters.Select(p=>p.Type.FullName);
    public Option<MethodInfo> MethodInfo => (Adaptee as RuntimeBindingMethod).MethodInfo;
}

public record StepDefinitionBindingAdapter(IStepDefinitionBinding Adaptee)
{
    public string StepDefinitionType => Adaptee.StepDefinitionType.ToString();
    public Option<Regex> Regex => Adaptee.Regex;
    public BindingMethodAdapter Method { get; } = new BindingMethodAdapter(Adaptee.Method);
    public bool IsScoped => Adaptee.IsScoped;
    public Option<string> BindingScopeTag => Adaptee.BindingScope.Tag;
    public string BindingScopeFeatureTitle => Adaptee.BindingScope.FeatureTitle;
    public string BindingScopeScenarioTitle => Adaptee.BindingScope.ScenarioTitle;
    public virtual Option<T> GetProperty<T>(string parameterName) => None<T>.Value;
}

public interface IBindingRegistryAdapter
{
    IEnumerable<StepDefinitionBindingAdapter> GetStepDefinitions(TestAssemblyLoadContext testAssemblyLoadContext, Option<FileDetails> configFile);
}

public class BindingRegistryAdapterAdapter : IBindingRegistryAdapter
{
    private readonly IBindingRegistryFactory _bindingRegistryFactory;


    public BindingRegistryAdapterAdapter(IBindingRegistryFactory bindingRegistryFactory)
    {
        _bindingRegistryFactory = bindingRegistryFactory;
    }

    public IEnumerable<StepDefinitionBindingAdapter> GetStepDefinitions(TestAssemblyLoadContext testAssemblyLoadContext,
        Option<FileDetails> configFile)
    {
        var bindingRegistry = _bindingRegistryFactory.GetBindingRegistry(testAssemblyLoadContext, configFile);
        var stepDefinitionBindings = bindingRegistry.GetStepDefinitions();
        return stepDefinitionBindings
            .Select(Adapt);
    }

    protected StepDefinitionBindingAdapter Adapt(IStepDefinitionBinding sd) 
        => new StepDefinitionBindingAdapter(sd);
}

public interface IBindingRegistryFactory
{
    IBindingRegistry GetBindingRegistry(TestAssemblyLoadContext testAssemblyLoadContext, Option<FileDetails> configFile);
}

public abstract class BindingRegistryFactory<TGlobalContainer> : IBindingRegistryFactory
{
    public IBindingRegistry GetBindingRegistry(TestAssemblyLoadContext testAssemblyLoadContext, Option<FileDetails> configFile)
    {
        var globalContainer = CreateGlobalContainer(testAssemblyLoadContext, configFile);
        RegisterTypeAs<NoInvokeDependencyProvider.NullBindingInvoker, IBindingInvoker>(globalContainer);
        //RegisterTypeAs<FakeTestContext, TestContext>(globalContainer);
        CreateTestRunner(globalContainer, testAssemblyLoadContext.TestAssembly);

        return ResolveBindingRegistry(testAssemblyLoadContext.TestAssembly, globalContainer);
    }

    protected abstract void RegisterTypeAs<TType, TInterface>(TGlobalContainer globalContainer) where TType : class, TInterface;
    protected abstract TGlobalContainer CreateGlobalContainer(TestAssemblyLoadContext testAssemblyLoadContext, Option<FileDetails> configFile);
    protected abstract void CreateTestRunner(TGlobalContainer globalContainer, Assembly testAssembly);
    protected abstract IBindingRegistry ResolveBindingRegistry(Assembly testAssembly, TGlobalContainer globalContainer);
}

public class BindingRegistryFactoryV3922 : BindingRegistryFactory<IObjectContainer>
{
    private readonly IFileSystem _fileSystem;

    public BindingRegistryFactoryV3922(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }

    protected override void RegisterTypeAs<TType, TInterface>(IObjectContainer globalContainer) 
        => globalContainer.RegisterTypeAs<TType, TInterface>();

    protected override IObjectContainer CreateGlobalContainer(TestAssemblyLoadContext testAssemblyLoadContext, Option<FileDetails> configFile)
    {
        var containerBuilder = new ContainerBuilder(new SpecFlowV31DependencyProvider(testAssemblyLoadContext));
        IConfigurationLoader configurationLoader = new SpecFlowConfigurationLoader(configFile, _fileSystem);
        var configurationProvider = new DefaultRuntimeConfigurationProvider(configurationLoader);
        return containerBuilder.CreateGlobalContainer(testAssemblyLoadContext.TestAssembly, configurationProvider);
    }

    protected override void CreateTestRunner(IObjectContainer globalContainer, Assembly testAssembly)
    {
        var testRunnerManager = (TestRunnerManager)globalContainer.Resolve<ITestRunnerManager>();

        testRunnerManager.Initialize(testAssembly);
        testRunnerManager.CreateTestRunner(0);
    }

    protected override IBindingRegistry ResolveBindingRegistry(Assembly testAssembly, IObjectContainer globalContainer)
        => globalContainer.Resolve<IBindingRegistry>();
}

public interface IRuntimeConfigurationProviderProxy
{
}

public class RuntimeConfigurationProviderProxyProxy : IRuntimeConfigurationProviderProxy
{
}
