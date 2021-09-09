using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using SpecFlow.VisualStudio.Discovery;
using SpecFlow.VisualStudio.UI.ViewModels;

namespace SpecFlow.VisualStudio.Editor.Commands
{
    internal class RenameStepPerformInStepDefinitionClass : RenameStepPerformBase
    {
        public override void PerformRenameStep(RenameStepViewModel viewModel, ITextBuffer textBufferOfStepDefinitionClass)
        {
            MethodDeclarationSyntax method = GetMethod(viewModel.SelectedStepDefinitionBinding, textBufferOfStepDefinitionClass);

            IOrderedEnumerable<SyntaxToken> expressionsToReplace = ExpressionsToReplace(viewModel, method);

            EditTextBuffer(textBufferOfStepDefinitionClass, expressionsToReplace, CalculateReplaceSpan, viewModel.StepText);

            viewModel.SelectedStepDefinitionProject.IdeScope.Logger.Log(TraceLevel.Info, method.AttributeLists.Count.ToString());
        }

        private static MethodDeclarationSyntax GetMethod(ProjectStepDefinitionBinding projectStepDefinitionBinding,
            ITextBuffer textBuffer)
        {
            Document roslynDocument = textBuffer.GetRelatedDocuments().Single();
            
            var rootNode = roslynDocument.GetSyntaxRootAsync().Result;
            var methodLine =
                textBuffer.CurrentSnapshot.GetLineFromLineNumber(projectStepDefinitionBinding.Implementation
                    .SourceLocation.SourceFileLine - 1);
            var methodColumn = projectStepDefinitionBinding.Implementation.SourceLocation.SourceFileColumn - 1;
            var methodPosition = methodLine.Start + methodColumn;
            var node = rootNode.FindNode(new TextSpan(methodPosition, 1));
            var method = node.Parent as MethodDeclarationSyntax;

            return method;
        }

        private static IOrderedEnumerable<SyntaxToken> ExpressionsToReplace(RenameStepViewModel viewModel, MethodDeclarationSyntax method)
        {
            var stepDefinitionAttributeTextTokens = method
                .AttributeLists
                .Select(al => al.Attributes.Single().ArgumentList.Arguments.Single().Expression.GetFirstToken())
                .Where(tok => tok.ValueText == viewModel.OriginalStepText)
                .OrderByDescending(tok => tok.SpanStart);
            return stepDefinitionAttributeTextTokens;
        }

        private static Span CalculateReplaceSpan(SyntaxToken token)
        {
            var offset = token.Text.IndexOf(token.ValueText, StringComparison.Ordinal);
            var trimLength = token.Text.Length - token.ValueText.Length;

            var replaceSpan = new Span(token.SpanStart + offset, token.Span.Length - trimLength);
            return replaceSpan;
        }

    }
}