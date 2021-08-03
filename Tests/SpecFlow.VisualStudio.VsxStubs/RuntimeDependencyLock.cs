using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpecFlow.VisualStudio.VsxStubs
{
    public class RuntimeDependencyLock
    {
        internal class MicrosoftVisualStudioPlatformVsEditor
        {
            private static void Dummy()
            {
                Noop(typeof(Microsoft.VisualStudio.Platform.VSEditor.SnapshotSpanEventArgsHelper));
            }
            private static void Noop(Type _) { }
        }
        internal class MicrosoftVisualStudioTextInternal
        {
            private static void Dummy()
            {
                Noop(typeof(Microsoft.VisualStudio.Text.Internal.Language.CompletionPresenterStylePrivate));
            }
            private static void Noop(Type _) { }
        }
    }
}
