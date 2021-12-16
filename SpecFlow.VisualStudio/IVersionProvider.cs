using System;
using System.Linq;

namespace SpecFlow.VisualStudio;

public interface IVersionProvider
{
    string GetVsVersion();
    string GetExtensionVersion();
}
