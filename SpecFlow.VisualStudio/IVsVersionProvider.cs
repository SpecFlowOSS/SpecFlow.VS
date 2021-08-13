using System;
using System.Linq;

namespace SpecFlow.VisualStudio
{
    public interface IVsVersionProvider
    {
        string GetVersion();
    }
}
