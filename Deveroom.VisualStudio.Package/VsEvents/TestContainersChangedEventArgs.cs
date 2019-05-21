using System;

namespace Deveroom.VisualStudio.VsEvents
{
    public class TestContainersChangedEventArgs : EventArgs
    {
        public bool Succeeded { get; private set; }

        public bool Canceled { get; private set; }

        public TestContainersChangedEventArgs(bool testContainerUpdateSucceeded, bool testContainerUpdateCanceled)
        {
            this.Succeeded = testContainerUpdateSucceeded;
            this.Canceled = testContainerUpdateCanceled;
        }
    }
}