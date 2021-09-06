using System;
using System.Linq;
using System.Windows;
using SpecFlow.VisualStudio.UI.ViewModels;
using Microsoft.VisualStudio.Shell.Interop;

namespace SpecFlow.VisualStudio.UI.Dialogs
{
    public partial class RenameStepDialog
    {
        public RenameStepViewModel ViewModel { get; }

        public RenameStepDialog()
        {
            InitializeComponent();
        }

        public RenameStepDialog(RenameStepViewModel viewModel, IVsUIShell vsUiShell = null) : base(vsUiShell)
        {
            ViewModel = viewModel;
            InitializeComponent();
        }

        private void Rename_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}
