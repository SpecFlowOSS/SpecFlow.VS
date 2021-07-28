using System;
using System.Linq;
using Deveroom.VisualStudio.UI.ViewModels;
using Deveroom.VisualStudio.UI.ViewModels.WizardDialogs;
using Microsoft.VisualStudio.Shell.Interop;

namespace Deveroom.VisualStudio.UI.Dialogs
{
    public partial class WelcomeDialog
    {
        public WizardViewModel ViewModel { get; }

        public WelcomeDialog()
        {
            InitializeComponent();
        }

        public WelcomeDialog(WizardViewModel viewModel, IVsUIShell vsUiShell = null) : base(vsUiShell)
        {
            ViewModel = viewModel;
            InitializeComponent();
        }
    }
}
