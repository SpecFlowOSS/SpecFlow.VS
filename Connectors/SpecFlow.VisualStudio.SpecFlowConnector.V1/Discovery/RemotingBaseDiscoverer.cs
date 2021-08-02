using System;
using System.Linq;
using System.Reflection;
using SpecFlow.VisualStudio.SpecFlowConnector.SourceDiscovery;
using SpecFlow.VisualStudio.SpecFlowConnector.SourceDiscovery.Com;
using SpecFlow.VisualStudio.SpecFlowConnector.SourceDiscovery.DnLib;

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
    }
}
