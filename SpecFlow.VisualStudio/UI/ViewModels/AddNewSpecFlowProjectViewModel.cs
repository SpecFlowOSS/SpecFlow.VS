using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using SpecFlow.VisualStudio.Annotations;

namespace SpecFlow.VisualStudio.UI.ViewModels
{
    public class AddNewSpecFlowProjectViewModel : INotifyPropertyChanged
    {
        private const string Runner = "SpecFlow + Runner";
        private const string Net6 = "net6.0";

        public string DotNetFramework
        {
            get => _dotNetFramework;
            set { 

                _dotNetFramework = value;
                if (_dotNetFramework == Net6 && TestFrameworks.Contains(Runner))
                {
                    TestFrameworks.Remove(Runner);
                    UnitTestFramework = TestFrameworks[0];
                    OnPropertyChanged(nameof(UnitTestFramework));
                }

                if (_dotNetFramework != Net6 && !TestFrameworks.Contains(Runner))
                {
                    TestFrameworks.Add(Runner);
                }
                OnPropertyChanged(nameof(TestFrameworks));
            }
        }

        public string UnitTestFramework { get; set; } = Runner;
        public bool FluentAssertionsIncluded { get; set; } = true;

#if DEBUG
        public static AddNewSpecFlowProjectViewModel DesignData = new ()
        {
            DotNetFramework = Net6,
            UnitTestFramework = Runner,
            FluentAssertionsIncluded = true,
        };
#endif
        private string _dotNetFramework = Net6;
        public ObservableCollection<string> TestFrameworks { get; } = new (new List<string> { "NUnit", "xUnit", "MSTest" });

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
