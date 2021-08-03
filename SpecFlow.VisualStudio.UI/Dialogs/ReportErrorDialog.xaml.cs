using System;
using System.Linq;
using System.Windows;
using SpecFlow.VisualStudio.UI.ViewModels;
using Microsoft.VisualStudio.Shell.Interop;

namespace SpecFlow.VisualStudio.UI.Dialogs
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
