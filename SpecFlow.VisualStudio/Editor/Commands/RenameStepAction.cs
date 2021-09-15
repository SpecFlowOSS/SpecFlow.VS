using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.Text;
using SpecFlow.VisualStudio.UI.ViewModels;

namespace SpecFlow.VisualStudio.Editor.Commands
{
    internal abstract class RenameStepAction : IRenameStepAction
    {
        public abstract bool PerformRenameStep(RenameStepViewModel viewModel,
            ITextBuffer textBufferOfStepDefinitionClass);

        protected static void EditTextBuffer<T>(
            ITextBuffer textBuffer,
            IEnumerable<T> expressionsToReplace,
            Func<T, Span> calculateReplaceSpan,
            string replacementText)
        {
            EditTextBuffer(textBuffer, expressionsToReplace, calculateReplaceSpan, _ => replacementText);
        }

        protected static void EditTextBuffer<T>(
            ITextBuffer textBuffer,
            IEnumerable<T> expressionsToReplace,
            Func<T, Span> calculateReplaceSpan,
            Func<T, string> calculateReplacementText)
        {
            using var textEdit = textBuffer.CreateEdit();

            foreach (var token in expressionsToReplace)
            {
                var replaceSpan = calculateReplaceSpan(token);
                textEdit.Replace(replaceSpan, calculateReplacementText(token));
            }

            textEdit.Apply();
        }
    }
}