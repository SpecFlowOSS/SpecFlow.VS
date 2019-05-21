using System;
using System.Text;

namespace Deveroom.VisualStudio.SpecFlowConnector
{
    class Program
    {
        static int Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            return new ConsoleRunner().EntryPoint(args);
        }
    }
}
