using System;

namespace SpecFlow.VisualStudio.VsEvents
{
    public class BuildCommandEventArgs : EventArgs
    {
        public bool IsBuildClean { get; private set; }

        public BuildCommandEventArgs(bool isBuildClean)
        {
            this.IsBuildClean = isBuildClean;
        }
    }
}