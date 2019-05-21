using System;
using System.Linq;
using System.Windows;
using Deveroom.VisualStudio.UI.ViewModels;
using Microsoft.VisualStudio.Shell.Interop;

namespace Deveroom.VisualStudio.UI.Dialogs
{
    public partial class ReportErrorDialog
    {
        public ReportErrorDialogViewModel ViewModel { get; }

        public ReportErrorDialog()
        {
            InitializeComponent();
        }

        public ReportErrorDialog(ReportErrorDialogViewModel viewModel, IVsUIShell vsUiShell = null) : base(vsUiShell)
        {
            ViewModel = viewModel;
            InitializeComponent();
        }

        private void CopyToClipboard_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.CopyErrorToClipboard();
        }
    }
}
