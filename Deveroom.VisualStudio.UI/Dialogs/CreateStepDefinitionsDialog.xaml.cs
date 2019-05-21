using System;
using System.Linq;
using System.Windows;
using Deveroom.VisualStudio.UI.ViewModels;
using Microsoft.VisualStudio.Shell.Interop;

namespace Deveroom.VisualStudio.UI.Dialogs
{
    public partial class CreateStepDefinitionsDialog
    {
        public CreateStepDefinitionsDialogViewModel ViewModel { get; }

        public CreateStepDefinitionsDialog()
        {
            InitializeComponent();
        }

        public CreateStepDefinitionsDialog(CreateStepDefinitionsDialogViewModel viewModel, IVsUIShell vsUiShell = null) : base(vsUiShell)
        {
            ViewModel = viewModel;
            viewModel.Result = CreateStepDefinitionsDialogResult.Cancel;
            InitializeComponent();
        }

        private void Create_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.Result = CreateStepDefinitionsDialogResult.Create;
            Close();
        }

        private void CopyToClipboard_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.Result = CreateStepDefinitionsDialogResult.CopyToClipboard;
            Close();
        }
    }
}
