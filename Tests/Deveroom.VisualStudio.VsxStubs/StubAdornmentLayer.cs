using System;
using System.Collections.ObjectModel;
using System.Windows;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Deveroom.VisualStudio.VsxStubs
{
    public class StubAdornmentLayer : IAdornmentLayer
    {
        public bool AddAdornment(AdornmentPositioningBehavior behavior, SnapshotSpan? visualSpan, object tag, UIElement adornment,
            AdornmentRemovedCallback removedCallback)
        {
            throw new NotImplementedException();
        }

        public bool AddAdornment(SnapshotSpan visualSpan, object tag, UIElement adornment)
        {
            throw new NotImplementedException();
        }

        public void RemoveAdornment(UIElement adornment)
        {
            throw new NotImplementedException();
        }

        public void RemoveAdornmentsByTag(object tag)
        {
            throw new NotImplementedException();
        }

        public void RemoveAdornmentsByVisualSpan(SnapshotSpan visualSpan)
        {
            throw new NotImplementedException();
        }

        public void RemoveMatchingAdornments(Predicate<IAdornmentLayerElement> match)
        {
            throw new NotImplementedException();
        }

        public void RemoveMatchingAdornments(SnapshotSpan visualSpan, Predicate<IAdornmentLayerElement> match)
        {
            throw new NotImplementedException();
        }

        public void RemoveAllAdornments()
        {
            throw new NotImplementedException();
        }

        public IWpfTextView TextView
        {
            get { throw new NotImplementedException(); }
        }

        public bool IsEmpty
        {
            get { throw new NotImplementedException(); }
        }

        public double Opacity { get; set; } = 0;

        public ReadOnlyCollection<IAdornmentLayerElement> Elements
        {
            get { throw new NotImplementedException(); }
        }
    }
}
