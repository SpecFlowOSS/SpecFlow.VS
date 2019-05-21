using System;
using System.Linq;
using System.Windows.Controls;

namespace Deveroom.VisualStudio.UI
{
    public interface IUiResourceProvider
    {
        Image GetIcon(string name);
    }
}
