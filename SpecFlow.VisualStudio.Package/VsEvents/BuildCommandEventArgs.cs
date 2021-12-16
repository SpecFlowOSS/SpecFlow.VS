using System;

namespace SpecFlow.VisualStudio.VsEvents;

public class BuildCommandEventArgs : EventArgs
{
    public BuildCommandEventArgs(bool isBuildClean)
    {
        IsBuildClean = isBuildClean;
    }

    public bool IsBuildClean { get; }
}
