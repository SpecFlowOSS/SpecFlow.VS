using System;
using System.Diagnostics;
using System.IO;
using Deveroom.VisualStudio.Common;
using SpecFlow.VisualStudio.SpecFlowConnector.Discovery;

namespace SpecFlow.VisualStudio.SpecFlowConnector
{
    public static class DiscoveryCommand
    {
        public const string CommandName = "discovery";

        public static string Execute(string[] commandArgs)
        {
            var options = DiscoveryOptions.Parse(commandArgs);
            var processor = new DiscoveryProcessor(options);
            return processor.Process();
        }
    }
}
