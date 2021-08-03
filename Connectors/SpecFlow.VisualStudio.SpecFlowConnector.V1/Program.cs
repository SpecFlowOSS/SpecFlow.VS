using System;
using System.Text;
using SpecFlow.VisualStudio.SpecFlowConnector.AppDomainHelper;

namespace SpecFlow.VisualStudio.SpecFlowConnector
{
    class Program
    {
        [STAThread]
        static int Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            using (AssemblyHelper.SubscribeResolveForAssembly(typeof(Program)))
                return new ConsoleRunner().EntryPoint(args);
        }
    }
}
