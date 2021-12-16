#nullable disable
using System.Windows;
using System.Windows.Controls;
using Microsoft.VisualStudio.Shell.Interop;
using SpecFlow.VisualStudio.UI.ViewModels;

namespace SpecFlow.VisualStudio.UI.Dialogs;

/// <summary>
///     Interaction logic for AddNewSpecFlowProjectDialog.xaml
/// </summary>
public partial class AddNewSpecFlowProjectDialog
{
    public AddNewSpecFlowProjectDialog()
    {
        InitializeComponent();
    }

    public AddNewSpecFlowProjectDialog(AddNewSpecFlowProjectViewModel viewModel, IVsUIShell vsUiShell = null) :
        base(vsUiShell)
    {
        ViewModel = viewModel;
        InitializeComponent();
    }

    public AddNewSpecFlowProjectViewModel ViewModel { get; }

    private void CreateButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void TestFramework_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count == 0) return;
        ViewModel.UnitTestFramework = e.AddedItems[0].ToString();
        e.Handled = true;
    }
}
