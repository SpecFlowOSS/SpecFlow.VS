using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpecFlow.VisualStudio.UI.ViewModels
{
    public class AddNewSpecFlowProjectViewModel
    {
        public string DotNetFramework { get; set; } = "netcoreapp3.1";
        public string UnitTestFramework { get; set; } = "runner";
        public bool FluentAssertionsIncluded { get; set; } = true;

#if DEBUG
        public static AddNewSpecFlowProjectViewModel DesignData = new ()
        {
            DotNetFramework = "netcoreapp3.1",
            UnitTestFramework = "runner",
            FluentAssertionsIncluded = true
        };
#endif
    }
}
