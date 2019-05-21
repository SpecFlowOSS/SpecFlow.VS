using System;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace Deveroom.VisualStudio.SpecFlowVsCompatibility
{
    [Export(typeof(IClassifierProvider))]
    [ContentType("gherkin")]
    public class SpecFlowVsCompatibilityClassifierProvider : IClassifierProvider
    {
        private readonly SpecFlowVsCompatibilityService _compatibilityService;

        [ImportingConstructor]
        public SpecFlowVsCompatibilityClassifierProvider(SpecFlowVsCompatibilityService compatibilityService)
        {
            _compatibilityService = compatibilityService;
        }

        public IClassifier GetClassifier(ITextBuffer textBuffer)
        {
            _compatibilityService.CheckCompatibilityOnce();
            return null;
        }
    }
}
