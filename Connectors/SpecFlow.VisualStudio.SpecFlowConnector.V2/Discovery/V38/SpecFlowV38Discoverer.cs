using System;
using System.Runtime.Loader;
using BoDi;
using SpecFlow.VisualStudio.SpecFlowConnector.Discovery.V31;
using TechTalk.SpecFlow.Infrastructure;
using TechTalk.SpecFlow.Plugins;
using TechTalk.SpecFlow.Tracing;

namespace SpecFlow.VisualStudio.SpecFlowConnector.Discovery.V38
{
    public class SpecFlowV38Discoverer : SpecFlowV31Discoverer
    {
        class ContainerBuilderThatResetsTraceListener : ContainerBuilder
        {
            public ContainerBuilderThatResetsTraceListener(IDefaultDependencyProvider defaultDependencyProvider = null) : base(defaultDependencyProvider)
            {
            }

            public override IObjectContainer CreateTestThreadContainer(IObjectContainer globalContainer)
            {
                var testThreadContainer = base.CreateTestThreadContainer(globalContainer);
                testThreadContainer.ReflectionRegisterTypeAs<NullListener, ITraceListener>();
                return testThreadContainer;
            }
        }

        public SpecFlowV38Discoverer(AssemblyLoadContext loadContext) : base(loadContext)
        {
        }

        protected override ContainerBuilder CreateContainerBuilder(DefaultDependencyProvider defaultDependencyProvider)
        {
            return new ContainerBuilderThatResetsTraceListener(defaultDependencyProvider);
        }
    }
}
