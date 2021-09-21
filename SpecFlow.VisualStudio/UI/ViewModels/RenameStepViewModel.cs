using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using SpecFlow.VisualStudio.Editor.Services.StepDefinitions;
using SpecFlow.VisualStudio.Annotations;
using SpecFlow.VisualStudio.Discovery;
using SpecFlow.VisualStudio.Editor.Services.Parser;

namespace SpecFlow.VisualStudio.UI.ViewModels
{
    public class RenameStepViewModel : INotifyPropertyChanged
    {
        public RenameStepViewModel(ProjectStepDefinitionBinding binding, Func<string, ImmutableHashSet<string>> validateFunc)
        {
            _validateFunc = validateFunc;
            StepText = binding.Expression;
            OriginalStepText = binding.ToString();
        }

        private readonly Func<string, ImmutableHashSet<string>> _validateFunc;
        
        public bool IsValid => string.IsNullOrEmpty(ValidationError);

        private string _validationError = string.Empty;
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

        private string _stepText;
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

        public AnalyzedStepDefinitionExpression ParsedUpdatedExpression { get; set; }

        private void Validate()
        {
            var errors = _validateFunc(StepText);
            ValidationError = string.Join(Environment.NewLine, errors);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

#if DEBUG
        public static RenameStepViewModel DesignData = new(
            new ProjectStepDefinitionBinding(
                ScenarioBlock.Given, 
                new Regex("^invalid$"), 
                new Scope(), 
                new ProjectStepDefinitionImplementation(
                    "WhenIPressAdd", 
                    Array.Empty<string>(), 
                    new SourceLocation("Steps.cs", 10, 9)) 
                ), StubValidation);

        public static ImmutableHashSet<string> StubValidation(string updatedExpression)
        {
            return updatedExpression == "invalid" 
                ? ImmutableHashSet.Create("This is wrong") 
                : ImmutableHashSet<string>.Empty;
        }
#endif
    }
}
