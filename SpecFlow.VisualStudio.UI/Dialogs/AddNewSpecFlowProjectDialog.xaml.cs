using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.VisualStudio.Shell.Interop;
using SpecFlow.VisualStudio.UI.ViewModels;

namespace SpecFlow.VisualStudio.UI.Dialogs
{
    /// <summary>
    /// Interaction logic for AddNewSpecFlowProjectDialog.xaml
    /// </summary>
    public partial class AddNewSpecFlowProjectDialog
    {

        public AddNewSpecFlowProjectViewModel ViewModel { get; }

        public AddNewSpecFlowProjectDialog()
        {
            InitializeComponent();
        }

        public AddNewSpecFlowProjectDialog(AddNewSpecFlowProjectViewModel viewModel, IVsUIShell vsUiShell = null) : base(vsUiShell)
        {
            ViewModel = viewModel;
            InitializeComponent();
        }
    }
}
