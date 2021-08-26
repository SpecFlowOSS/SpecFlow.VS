using System;
using System.ComponentModel.Composition;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.Shell;

namespace SpecFlow.VisualStudio
{
    [Export(typeof(IVersionProvider))]
    public class VersionProvider : IVersionProvider
    {
        private readonly IServiceProvider _serviceProvider;

        private readonly Lazy<string> _lazyVsVersion;
        private readonly Lazy<string> _lazyExtensionVersion;

        [ImportingConstructor]
        public VersionProvider([Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _lazyVsVersion = new Lazy<string>(GetVsProductDisplayVersion);
            _lazyExtensionVersion = new Lazy<string>(ReadExtensionVersion);
        }

        public string GetVsVersion()
        {
            return _lazyVsVersion.Value;
        }

        private string GetVsProductDisplayVersion()
        {
            return VsUtils.GetVsProductDisplayVersionSafe(_serviceProvider);
        }

        public string GetExtensionVersion()
        {
            return _lazyExtensionVersion.Value;
        }

        private string ReadExtensionVersion()
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
