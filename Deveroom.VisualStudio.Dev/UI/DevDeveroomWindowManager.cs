using System;
using System.ComponentModel.Composition;
using System.Linq;
using Deveroom.VisualStudio.ProjectSystem;
using Deveroom.VisualStudio.UI;
using Microsoft.VisualStudio.Shell;

namespace Deveroom.VisualStudio.Dev.UI
{
    [Export(typeof(IDeveroomWindowManager))]
    public class DevDeveroomWindowManager : DeveroomWindowManager
    {
        [ImportingConstructor]
        public DevDeveroomWindowManager([Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }
    }
}
