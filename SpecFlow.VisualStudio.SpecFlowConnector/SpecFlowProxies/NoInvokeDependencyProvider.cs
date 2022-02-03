﻿using BoDi;
using TechTalk.SpecFlow.Bindings;
using TechTalk.SpecFlow.Infrastructure;
using TechTalk.SpecFlow.Tracing;

namespace SpecFlowConnector.SpecFlowProxies;

public class NoInvokeDependencyProvider : DefaultDependencyProvider
{
    public override void RegisterGlobalContainerDefaults(ObjectContainer container)
    {
        base.RegisterGlobalContainerDefaults(container);
       // container.ReflectionRegisterTypeAs<NullBindingInvoker, IBindingInvoker>();
        container.RegisterTypeAs<NullBindingInvoker, IBindingInvoker>();
    }

    // ReSharper disable once ClassNeverInstantiated.Local
    public class NullBindingInvoker : IBindingInvoker
    {
        public object InvokeBinding(IBinding binding, IContextManager contextManager, object[] arguments,
            ITestTracer testTracer, out TimeSpan duration)
        {
            duration = TimeSpan.Zero;
            return null!;
        }
    }
}