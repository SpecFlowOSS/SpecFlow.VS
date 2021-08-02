using System;
using System.Collections.Generic;
using System.Linq;
using SpecFlow.VisualStudio.ProjectSystem;

namespace SpecFlow.VisualStudio.VsxStubs.ProjectSystem
{
    public class StubWindowManager : IDeveroomWindowManager
    {
        private readonly Dictionary<Type, object> _showDialogViewModels = new Dictionary<Type, object>();
        private readonly List<Tuple<Type, Delegate>> _registeredActions = new List<Tuple<Type, Delegate>>();

        public TViewModel GetShowDialogViewModel<TViewModel>() where TViewModel: class
        {
            if (!_showDialogViewModels.TryGetValue(typeof(TViewModel), out var viewModel))
                return null;

            return (TViewModel)viewModel;
        }

        public bool? ShowDialog<TViewModel>(TViewModel viewModel)
        {
            _showDialogViewModels[typeof(TViewModel)] = viewModel;
            foreach (var action in _registeredActions.Where(a => a.Item1 == typeof(TViewModel)).Select(a => a.Item2))
            {
                action.DynamicInvoke(viewModel);
            }
            return true;
        }

        public void RegisterWindowAction<TViewModel>(Action<TViewModel> action)
        {
            _registeredActions.Add(new Tuple<Type, Delegate>(typeof(TViewModel), action));
        }
    }
}
