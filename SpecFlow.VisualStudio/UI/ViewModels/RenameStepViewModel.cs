using System.ComponentModel;
using System.Runtime.CompilerServices;
using SpecFlow.VisualStudio.Discovery;
using SpecFlow.VisualStudio.Editor.Services.StepDefinitions;
using SpecFlow.VisualStudio.ProjectSystem;
using SpecFlow.VisualStudio.Annotations;

namespace SpecFlow.VisualStudio.UI.ViewModels
{
    public class RenameStepViewModel : INotifyPropertyChanged
    {
        public bool IsValid => ValidationError != null;

        public string ValidationError
        {
            get => _validationError;
            set
            {
                if (value == _validationError) return;
                _validationError = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsValid));
            }
        }

        public string StepText
        {
            get => _stepText;
            set
            {
                _stepText = value;
                Validate();
            }
        }

        public string OriginalStepText { get; }

        public IProjectScope SelectedStepDefinitionProject { get; }

        public ProjectStepDefinitionBinding SelectedStepDefinitionBinding { get; }
        public AnalyzedStepDefinitionExpression AnalyzedOriginalExpression { get; }
        public IStepDefinitionExpressionAnalyzer StepDefinitionExpressionAnalyzer { get; }
        public AnalyzedStepDefinitionExpression ParsedUpdatedExpression { get; set; }

        private void Validate()
        {
            ValidationError = null;
            if (StepText == "invalid")
            {
                ValidationError = "bla bla";
            }
        }

        public RenameStepViewModel(string stepText, IProjectScope selectedStepDefinitionProject,
            ProjectStepDefinitionBinding selectedStepDefinitionBinding,
            AnalyzedStepDefinitionExpression analyzedOriginalExpression,
            IStepDefinitionExpressionAnalyzer stepDefinitionExpressionAnalyzer)
        {
            StepText = stepText;
            SelectedStepDefinitionProject = selectedStepDefinitionProject;
            SelectedStepDefinitionBinding = selectedStepDefinitionBinding;
            AnalyzedOriginalExpression = analyzedOriginalExpression;
            StepDefinitionExpressionAnalyzer = stepDefinitionExpressionAnalyzer;
            OriginalStepText = stepText;
        }

#if DEBUG
        public static RenameStepViewModel DesignData = new("I press add", null, null, null, null)
        {
            ValidationError = "This is wrong"
        };
        private string _stepText;
        private string _validationError = null;
#endif
        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
