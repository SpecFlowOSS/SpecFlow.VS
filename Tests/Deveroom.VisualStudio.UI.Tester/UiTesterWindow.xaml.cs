using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Deveroom.VisualStudio.ProjectSystem.Actions;
using Deveroom.VisualStudio.UI.ViewModels;
using Deveroom.VisualStudio.UI.Dialogs;

namespace Deveroom.VisualStudio.UI.Tester
{
    /// <summary>
    /// Interaction logic for UiTesterWindow.xaml
    /// </summary>
    public partial class UiTesterWindow : Window
    {
        private readonly ContextMenuManager _contextMenuManager = new ContextMenuManager(UiResourceProvider.Instance);

        public UiTesterWindow()
        {
            InitializeComponent();
        }

        private ContextMenu CreateContextMenu()
        {
            var command = new Action<ContextMenuItem>(item => MessageBox.Show($"hello {item.Label}", "test"));
            var contextMenu = _contextMenuManager.CreateContextMenu("Test context menu",
                new ContextMenuItem("Defined step [regex]", command, "StepDefinitionsDefined"),
                new ContextMenuItem("Invalid defined step [regex]", command, "StepDefinitionsDefinedInvalid"),
                new ContextMenuItem("Ambiguous step [regex]", command, "StepDefinitionsAmbiguous"),
                new ContextMenuItem("Undefined step", command, "StepDefinitionsUndefined"));
            return contextMenu;
        }

        private void Test_ContextMenu_DefaultLocation(object sender, RoutedEventArgs e)
        {
            var contextMenu = CreateContextMenu();
            _contextMenuManager.ShowContextMenu(contextMenu);
        }

        private void Test_ContextMenu_At100x100(object sender, RoutedEventArgs e)
        {
            var contextMenu = CreateContextMenu();
            _contextMenuManager.ShowContextMenu(contextMenu, new Point(100,100));
        }

        private void Test_GenerateStepDefinitions(object sender, RoutedEventArgs e)
        {
            var viewModel = CreateStepDefinitionsDialogViewModel.DesignData;
            var dialog = new CreateStepDefinitionsDialog(viewModel);
            dialog.ShowDialog();

            string resultMessage =
                $"{viewModel.Result}: ClassName={viewModel.ClassName}, Snippets={string.Join(",", viewModel.Items.Select((item, i) => item.IsSelected ? i.ToString() : null).Where(i => i != null))}";

            MessageBox.Show(resultMessage);
        }

        private void Test_ReportError(object sender, RoutedEventArgs e)
        {
            var viewModel = ReportErrorDialogViewModel.DesignData;
            var dialog = new ReportErrorDialog(viewModel);
            dialog.ShowDialog();
        }
    }
}
