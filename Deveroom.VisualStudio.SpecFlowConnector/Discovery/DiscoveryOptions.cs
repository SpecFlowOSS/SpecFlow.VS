using System;
using System.IO;
using Deveroom.VisualStudio.Common;

namespace Deveroom.VisualStudio.SpecFlowConnector.Discovery
{
    [Serializable]
    public class DiscoveryOptions
    {
        public string AssemblyFilePath { get; private set; }
        public string ConfigFilePath { get; private set; }

        public string TargetFolder { get; private set; }
        public string ConnectorFolder { get; private set; }

        public static DiscoveryOptions Parse(string[] args)
        {
            var options = new DiscoveryOptions
            {
                AssemblyFilePath = Path.GetFullPath(args[0]),
                ConfigFilePath = string.IsNullOrWhiteSpace(args[1]) ? null : Path.GetFullPath(args[1])
            };

            options.TargetFolder = Path.GetDirectoryName(options.AssemblyFilePath);
            if (options.TargetFolder == null)
                throw new InvalidOperationException($"Unable to detect target folder from test assembly path '{options.AssemblyFilePath}'");
            options.ConnectorFolder = Path.GetDirectoryName(typeof(ConsoleRunner).Assembly.GetLocalCodeBase());
            if (options.ConnectorFolder == null)
                throw new InvalidOperationException($"Unable to detect connector folder.");

            return options;
        }
    }
}
