using System;
using System.Linq;
using System.Windows;
using Microsoft.VisualStudio.Shell.Interop;
using SpecFlow.VisualStudio.UI.ViewModels;

namespace SpecFlow.VisualStudio.UI.Dialogs;

public partial class CreateStepDefinitionsDialog
{
    public CreateStepDefinitionsDialog()
    {
        InitializeComponent();
    }

    public CreateStepDefinitionsDialog(CreateStepDefinitionsDialogViewModel viewModel, IVsUIShell vsUiShell = null) :
        base(vsUiShell)
    {
        ViewModel = viewModel;
        viewModel.Result = CreateStepDefinitionsDialogResult.Cancel;
        InitializeComponent();
    }

    public CreateStepDefinitionsDialogViewModel ViewModel { get; }

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
