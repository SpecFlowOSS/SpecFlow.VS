using System;
using System.Linq;
using Deveroom.VisualStudio.ProjectSystem;
using Deveroom.VisualStudio.UI.ViewModels;
using Deveroom.VisualStudio.UI.Dialogs;
using Microsoft.VisualStudio.Shell.Interop;

namespace Deveroom.VisualStudio.UI
{
    //https://stackoverflow.com/questions/40608094/wpf-modal-window-in-visual-studio-extension-blocking-input
    //https://stackoverflow.com/questions/38057256/wpf-dialog-blocking-input-after-closing?rq=1
    public class DeveroomWindowManager : IDeveroomWindowManager
    {
        protected readonly IVsUIShell _vsUiShell;

        public DeveroomWindowManager(IServiceProvider serviceProvider)
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
            if ((object)viewModel is CreateStepDefinitionsDialogViewModel createStepDefinitionsDialogViewModel)
                return new CreateStepDefinitionsDialog(createStepDefinitionsDialogViewModel, _vsUiShell);

            if ((object)viewModel is ReportErrorDialogViewModel reportErrorDialogViewModel)
                return new ReportErrorDialog(reportErrorDialogViewModel, _vsUiShell);

            throw new NotSupportedException(typeof(TViewModel).ToString());
        }
    }
}
