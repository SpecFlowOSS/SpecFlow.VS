using System;

namespace SpecFlow.VisualStudio.VsEvents;

public class HostOpenedEventArgs : EventArgs
{
    public HostOpenedEventArgs(string fileName)
    {
        FileName = fileName;
    }

    public string FileName { get; }
}
