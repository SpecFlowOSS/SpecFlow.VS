using System;
using System.ComponentModel.Composition;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.Shell;
using SpecFlow.VisualStudio.Analytics;

namespace SpecFlow.VisualStudio
{
    [Export(typeof(IVersionProvider))]
    public class VersionProvider : IVersionProvider
    {
        private readonly IServiceProvider _serviceProvider;

        [ImportingConstructor]
        public VersionProvider([Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public string GetVsVersion()
        {
            var version = VsUtils.GetVsSemanticVersion(_serviceProvider);
            return version;
        }

        public string GetExtensionVersion()
        {
            var assembly = Assembly.GetAssembly(typeof(VersionProvider));
            var versionAttr = assembly.GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute)).OfType<AssemblyInformationalVersionAttribute>().FirstOrDefault();
            if (versionAttr != null)
            {
                return versionAttr.InformationalVersion.Split('+', '-')[0];
            }
            return assembly.GetName().Version.ToString(3);
        }
    }
}
