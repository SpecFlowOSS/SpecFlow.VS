using System;
using System.Linq;
using System.Windows.Controls;

namespace SpecFlow.VisualStudio.UI;

public interface IUiResourceProvider
{
    Image GetIcon(string name);
}
