using System;
using BoDi;
using TechTalk.SpecFlow.Bindings;
using TechTalk.SpecFlow.Infrastructure;
using TechTalk.SpecFlow.Tracing;

namespace SpecFlow.VisualStudio.SpecFlowConnector.Discovery
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
            ContainerRegisterTypeAs<NullBindingInvoker, IBindingInvoker>(container);
        }

        protected void ContainerRegisterTypeAs<TType, TInterface>(ObjectContainer container)
        {
            // need to call RegisterTypeAs through reflection, because BoDi 1.5 (used from SpecFlow 3.7) introduced a return type
            container.ReflectionCallMethod("RegisterTypeAs", typeof(TType), typeof(TInterface));
        }
    }
}