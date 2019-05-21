using System;
using BoDi;
using TechTalk.SpecFlow.Bindings;
using TechTalk.SpecFlow.Infrastructure;
using TechTalk.SpecFlow.Tracing;

namespace Deveroom.VisualStudio.SpecFlowConnector.Discovery
{
    public class NoInvokeDependencyProvider : DefaultDependencyProvider
    {
        // ReSharper disable once ClassNeverInstantiated.Local
        public class NullBindingInvoker : IBindingInvoker
        {
            public object InvokeBinding(IBinding binding, IContextManager contextManager, object[] arguments, ITestTracer testTracer, out TimeSpan duration)
            {
                duration = TimeSpan.Zero;
                return null;
            }
        }

        public override void RegisterGlobalContainerDefaults(ObjectContainer container)
        {
            base.RegisterGlobalContainerDefaults(container);
            container.RegisterTypeAs<NullBindingInvoker, IBindingInvoker>();
        }
    }
}