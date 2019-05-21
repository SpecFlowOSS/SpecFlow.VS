using System.ComponentModel;
using System.Runtime.CompilerServices;
using Deveroom.VisualStudio.Annotations;

namespace Deveroom.VisualStudio.UI.ViewModels.WizardDialogs
{
    public class WizardPageViewModel : INotifyPropertyChanged
    {
        private bool _isActive;
        public string Name { get; }

        public bool IsActive
        {
            get => _isActive;
            set
            {
                if (value == _isActive) return;
                _isActive = value;
                OnPropertyChanged();
            }
        }

        public WizardPageViewModel(string name)
        {
            Name = name;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}