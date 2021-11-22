﻿using System;
using System.Collections.Generic;
using System.Reflection;
using BoDi;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SpecFlow.VisualStudio.SpecFlowConnector.SourceDiscovery;
using SpecFlow.VisualStudio.SpecFlowConnector.SourceDiscovery.Com;
using SpecFlow.VisualStudio.SpecFlowConnector.SourceDiscovery.DnLib;
using TechTalk.SpecFlow;
using TechTalk.SpecFlow.Bindings;
using TechTalk.SpecFlow.Configuration;
using TechTalk.SpecFlow.Infrastructure;

namespace SpecFlow.VisualStudio.SpecFlowConnector.Discovery
{
    public abstract class RemotingBaseDiscoverer : BaseDiscoverer, IRemotingSpecFlowDiscoverer
    {
        public string Discover(string testAssemblyPath, string configFilePath)
        {
            var testAssembly = Assembly.LoadFrom(testAssemblyPath);
            return Discover(testAssembly, testAssemblyPath, configFilePath);
        }

        protected override IDeveroomSymbolReader CreateSymbolReader(string assemblyFilePath, WarningCollector warningCollector)
        {
            var symbolReaderFactories = new Func<string, IDeveroomSymbolReader>[]
            {
                path => new DnLibDeveroomSymbolReader(path),
                path => new ComDeveroomSymbolReader(path),
            };

            foreach (var symbolReaderFactory in symbolReaderFactories)
            {
                try
                {
                    return symbolReaderFactory(assemblyFilePath);
                }
                catch (Exception ex)
                {
                    warningCollector.AddWarning($"CreateSymbolReader({assemblyFilePath})", ex);
                }
            }
            return new NullDeveroomSymbolReader();
        }

        protected override IBindingRegistry GetBindingRegistry(Assembly testAssembly, string configFilePath)
        {
            var globalContainer = CreateGlobalContainer(testAssembly, configFilePath);
            RegisterTypeAs<NoInvokeDependencyProvider.NullBindingInvoker, IBindingInvoker>(globalContainer);
            RegisterTypeAs<FakeTestContext, TestContext>(globalContainer);

            return ResolveBindingRegistry(testAssembly, globalContainer);
        }

        protected abstract object CreateGlobalContainer(Assembly testAssembly, string configFilePath);

        protected abstract void RegisterTypeAs<TType, TInterface>(object globalContainer) where TType : class, TInterface;

        protected abstract T Resolve<T>(object globalContainer);

        protected abstract IBindingRegistry ResolveBindingRegistry(Assembly testAssembly, object globalContainer);

        protected override IEnumerable<IStepDefinitionBinding> GetStepDefinitions(IBindingRegistry bindingRegistry)
        {
            return bindingRegistry.GetStepDefinitions();
        }
    }
}
