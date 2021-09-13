#nullable enable
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using SpecFlow.VisualStudio.Discovery;
using SpecFlow.VisualStudio.UI.ViewModels;

namespace SpecFlow.VisualStudio.Editor.Commands
{
    internal class RenameStepStepDefinitionClassAction : RenameStepAction
    {
        public override bool PerformRenameStep(RenameStepViewModel viewModel,
            ITextBuffer textBufferOfStepDefinitionClass)
        {
            MethodDeclarationSyntax method = GetMethod(viewModel.SelectedStepDefinitionBinding, textBufferOfStepDefinitionClass);

            ImmutableSortedSet<SyntaxToken> expressionsToReplace = ExpressionsToReplace(viewModel, method);
            if (expressionsToReplace.IsEmpty) return false;

            EditTextBuffer(textBufferOfStepDefinitionClass, expressionsToReplace, CalculateReplaceSpan, viewModel.StepText);

            viewModel.SelectedStepDefinitionProject.IdeScope.Logger.Log(TraceLevel.Info, method.AttributeLists.Count.ToString());

            return true;
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

        private static ImmutableSortedSet<SyntaxToken> ExpressionsToReplace(RenameStepViewModel viewModel, MethodDeclarationSyntax method)
        {
            var stepDefinitionAttributeTextTokens = method
                .AttributeLists
                .Select(ArgumentTokens)
                .Where(tok => !tok.IsMissing && MatchesWithOriginalText(tok))
                .OrderByDescending(tok => tok.SpanStart)
                .ToImmutableSortedSet();

            return stepDefinitionAttributeTextTokens;

            bool MatchesWithOriginalText(SyntaxToken tok) => tok.ValueText == viewModel.OriginalStepText;
        }

        private static SyntaxToken ArgumentTokens(AttributeListSyntax al)
        {
            AttributeArgumentListSyntax? attributeArgumentListSyntax = al.Attributes.Single().ArgumentList;
            return attributeArgumentListSyntax == null || attributeArgumentListSyntax.Arguments.Count == 0
                ? SyntaxFactory.MissingToken(SyntaxKind.StringLiteralToken) 
                : attributeArgumentListSyntax.Arguments.Single().Expression.GetFirstToken();
        }

        private static Span CalculateReplaceSpan(SyntaxToken token)
        {
            var offset = IsVerbatim(token) ? 2 : 1;

            var replaceSpan = new Span(token.SpanStart + offset, token.Span.Length - offset - 1);
            return replaceSpan;
        }

        private static bool IsVerbatim(SyntaxToken token)
        {
            return token.Text.StartsWith("@");
        }
    }
}