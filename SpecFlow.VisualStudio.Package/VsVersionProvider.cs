using System;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.VisualStudio.Shell;

namespace SpecFlow.VisualStudio
{
    [Export(typeof(IVsVersionProvider))]
    public class VsVersionProvider : IVsVersionProvider
    {
        private readonly IServiceProvider _serviceProvider;

        [ImportingConstructor]
        public VsVersionProvider([Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public string GetVersion()
        {
            var version = VsUtils.GetVsSemanticVersion(_serviceProvider);
            return version;
        }
    }
}
