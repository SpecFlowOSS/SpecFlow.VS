using System;

namespace SpecFlow.VisualStudio.VsEvents
{
    public class HostOpenedEventArgs : EventArgs
    {
        public string FileName { get; private set; }

        public HostOpenedEventArgs(string fileName)
        {
            this.FileName = fileName;
        }
    }
}