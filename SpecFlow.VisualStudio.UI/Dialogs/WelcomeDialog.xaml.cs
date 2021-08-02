using System;
using System.Linq;
using SpecFlow.VisualStudio.UI.ViewModels;
using SpecFlow.VisualStudio.UI.ViewModels.WizardDialogs;
using Microsoft.VisualStudio.Shell.Interop;

namespace SpecFlow.VisualStudio.UI.Dialogs
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
