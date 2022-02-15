using System;
using System.Linq;

namespace SpecFlow.VisualStudio.SpecFlowConnector.Tests.AssemblyLoading;

internal class StubAssembly
{
    public Assembly Load(string path) => Assembly.LoadFrom(path);
}
