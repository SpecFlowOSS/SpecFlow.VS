// Decompiled with JetBrains decompiler
// Type: Microsoft.VisualStudio.TestWindow.VsHost.BuildCommandIntercepter
// Assembly: Microsoft.VisualStudio.TestWindow.Core, Version=15.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
// MVID: 87C699D5-75FE-4153-9738-59513196DBB5
// Assembly location: C:\Program Files (x86)\Microsoft Visual Studio\2017\Enterprise\Common7\IDE\CommonExtensions\Microsoft\TestWindow\Microsoft.VisualStudio.TestWindow.Core.dll

using System;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell.Interop;
using IServiceProvider = System.IServiceProvider;

namespace SpecFlow.VisualStudio.VsEvents
{
    internal sealed class BuildCommandIntercepter : IDisposable, IOleCommandTarget
    {
        private Guid programmaticBuildCmdGrp = new Guid("95A56312-F89A-4838-B533-9F0064C4AA63");
        private uint programmaticBuildCmdId = 1;
        private EventHandler<BuildCommandEventArgs> userInitiatedBuild;
        private uint cookie;
        private IVsRegisterPriorityCommandTarget registerCommandTarget;
        private static BuildCommandIntercepter instance;

        private BuildCommandIntercepter(IServiceProvider serviceProvider)
        {
            if (serviceProvider == null) throw new ArgumentNullException(nameof(serviceProvider));
            this.registerCommandTarget = serviceProvider.GetService<IVsRegisterPriorityCommandTarget>(typeof(SVsRegisterPriorityCommandTarget));
            ErrorHandler.ThrowOnFailure(this.registerCommandTarget.RegisterPriorityCommandTarget(0U, (IOleCommandTarget)this, out this.cookie));
        }

        public static BuildCommandIntercepter InitializeCommandInterceptor(IServiceProvider serviceProvider)
        {
            if (BuildCommandIntercepter.instance == null)
                BuildCommandIntercepter.instance = new BuildCommandIntercepter(serviceProvider);
            return BuildCommandIntercepter.instance;
        }

        internal event EventHandler<BuildCommandEventArgs> UserInitiatedBuild
        {
            add
            {
                this.userInitiatedBuild += value;
            }
            remove
            {
                this.userInitiatedBuild -= value;
            }
        }

        private static bool IsBuildCommand(Guid commandGroup, uint commandID)
        {
            if (commandGroup == (Guid)VSConstants.GUID_VSStandardCommandSet97)
            {
                switch (commandID)
                {
                    case 882:
                    case 883:
                    case 885:
                    case 886:
                    case 887:
                    case 889:
                    case 892:
                    case 893:
                    case 895:
                        return true;
                }
            }
            return false;
        }

        public bool IsBuildClean(Guid commandGroup, uint commandID)
        {
            return commandGroup == (Guid)VSConstants.GUID_VSStandardCommandSet97 && (commandID == 885U || commandID == 889U || commandID == 895U);
        }

        private bool IsProgrammaticBuildCmd(Guid pguidCmdGroup, uint nCmdID)
        {
            return pguidCmdGroup.Equals(this.programmaticBuildCmdGrp) && (int)nCmdID == (int)this.programmaticBuildCmdId;
        }

        int IOleCommandTarget.Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            if (pvaOut == IntPtr.Zero && (BuildCommandIntercepter.IsBuildCommand(pguidCmdGroup, nCmdID) || this.IsProgrammaticBuildCmd(pguidCmdGroup, nCmdID)))
            {
                EventHandler<BuildCommandEventArgs> userInitiatedBuild = this.userInitiatedBuild;
                if (userInitiatedBuild != null)
                {
                    BuildCommandEventArgs e = new BuildCommandEventArgs(this.IsBuildClean(pguidCmdGroup, nCmdID));
                    userInitiatedBuild((object)this, e);
                }
            }
            return -2147221248;
        }

        int IOleCommandTarget.QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
        {
            return -2147221248;
        }

        public void Dispose()
        {
            if (this.cookie == 0U || this.registerCommandTarget == null)
                return;
            ErrorHandler.Succeeded(this.registerCommandTarget.UnregisterPriorityCommandTarget(this.cookie));
            this.cookie = 0U;
        }
    }
}
