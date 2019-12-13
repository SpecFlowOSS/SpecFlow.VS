using System;
using System.Windows;
using Deveroom.VisualStudio.Diagonostics;
using Deveroom.VisualStudio.ProjectSystem.Actions;

namespace Deveroom.VisualStudio.ProjectSystem
{
    public class VsIdeActionsBase
    {
        protected readonly IVsIdeScope IdeScope;
        protected IDeveroomLogger Logger => IdeScope.Logger;

        public VsIdeActionsBase(IVsIdeScope ideScope)
        {
            IdeScope = ideScope;
        }

        public void ShowError(string description, Exception exception)
        {
            ReportErrorServices.ReportGeneralError(IdeScope, description, exception);
        }

        public void ShowProblem(string description, string title = null)
        {
            Logger.LogWarning($"User Notification: {description}");
            var caption = title == null ? "Deveroom for SpecFlow" : $"Deveroom: {title}";
            MessageBox.Show(description, caption, MessageBoxButton.OK, MessageBoxImage.Warning, MessageBoxResult.OK);
        }

        public void ShowQuestion(QuestionDescription questionDescription)
        {
            Logger.LogInfo($"User Question: {questionDescription.Description}");
            var caption = questionDescription.Title == null ? "Deveroom" : $"Deveroom: {questionDescription.Title}";
            var result = MessageBox.Show(questionDescription.Description, caption, questionDescription.IncludeCancel ? MessageBoxButton.YesNoCancel : MessageBoxButton.YesNo,
                MessageBoxImage.Question, questionDescription.NoCommandIsDefault ? MessageBoxResult.No : MessageBoxResult.Yes);
            if (result == MessageBoxResult.Yes)
                questionDescription.YesCommand?.Invoke(questionDescription);
            if (result == MessageBoxResult.No)
                questionDescription.NoCommand?.Invoke(questionDescription);
        }

        public void SetClipboardText(string text)
        {
            Clipboard.SetText(text);
        }
    }
}