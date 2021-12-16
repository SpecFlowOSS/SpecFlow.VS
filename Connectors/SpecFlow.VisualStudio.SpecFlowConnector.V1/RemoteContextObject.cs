using System;
using System.Security;

namespace SpecFlow.VisualStudio.SpecFlowConnector;

public class RemoteContextObject : MarshalByRefObject
{
    [SecurityCritical]
    public sealed override object InitializeLifetimeService() => null;
}
