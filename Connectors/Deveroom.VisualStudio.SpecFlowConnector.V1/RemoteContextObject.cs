using System;
using System.Security;

namespace Deveroom.VisualStudio.SpecFlowConnector
{
    public class RemoteContextObject : MarshalByRefObject
    {
        [SecurityCritical]
        public sealed override object InitializeLifetimeService()
        {
            return null;
        }
    }
}
