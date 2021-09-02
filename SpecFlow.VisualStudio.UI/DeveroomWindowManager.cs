using System;
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows.Navigation;
using SpecFlow.VisualStudio.ProjectSystem;
using SpecFlow.VisualStudio.UI.ViewModels;
using SpecFlow.VisualStudio.UI.Dialogs;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace SpecFlow.VisualStudio.UI
{
    //https://stackoverflow.com/questions/40608094/wpf-modal-window-in-visual-studio-extension-blocking-input
    //https://stackoverflow.com/questions/38057256/wpf-dialog-blocking-input-after-closing?rq=1
    [Export(typeof(IDeveroomWindowManager))]
    public class DeveroomWindowManager : IDeveroomWindowManager
    {
        protected readonly IVsUIShell _vsUiShell;

        [ImportingConstructor]
        public DeveroomWindowManager([Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider)
        {
            _vsUiShell = (IVsUIShell)serviceProvider.GetService(typeof(SVsUIShell));
        }

        public bool? ShowDialog<TViewModel>(TViewModel viewModel)
        {
            var window = CreateWindow(viewModel);
            return window.ShowModal();
        }

        protected virtual DialogWindow CreateWindow<TViewModel>(TViewModel viewModel)
        {
            var dialogWindow = CreateWindowInternal(viewModel);
            dialogWindow.LinkClicked += OnLinkClicked;
            return dialogWindow;
        }

        private void OnLinkClicked(object sender, RequestNavigateEventArgs e)
        {
            var uriString = e.Uri.ToString();
            if (!uriString.StartsWith("file"))
            {
                //todo: track link click if needed
            }
        }

        private DialogWindow CreateWindowInternal<TViewModel>(TViewModel viewModel)
        {
            if ((object)viewModel is CreateStepDefinitionsDialogViewModel createStepDefinitionsDialogViewModel)
                return new CreateStepDefinitionsDialog(createStepDefinitionsDialogViewModel, _vsUiShell);

            if ((object)viewModel is ReportErrorDialogViewModel reportErrorDialogViewModel)
                return new ReportErrorDialog(reportErrorDialogViewModel, _vsUiShell);

            if ((object)viewModel is WelcomeDialogViewModel welcomeDialogViewModel)
                return new WelcomeDialog(welcomeDialogViewModel, _vsUiShell);

            if ((object)viewModel is UpgradeDialogViewModel upgradeDialogViewModel)
                return new WelcomeDialog(upgradeDialogViewModel, _vsUiShell);

            if ((object)viewModel is AddNewSpecFlowProjectViewModel addNewSpecFlowProjectViewModel)
                return new AddNewSpecFlowProjectDialog(addNewSpecFlowProjectViewModel, _vsUiShell);

            throw new NotSupportedException(typeof(TViewModel).ToString());
        }
    }
}
