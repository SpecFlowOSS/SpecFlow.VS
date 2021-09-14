#nullable enable
using System;
using System.Collections.Generic;
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
            MethodDeclarationSyntax? method = GetMethod(viewModel.SelectedStepDefinitionBinding, textBufferOfStepDefinitionClass);
            if (method == null)
                return false;

            var expressionsToReplace = ExpressionsToReplace(viewModel, method);
            if (expressionsToReplace.Length == 0) return false;

            EditTextBuffer(textBufferOfStepDefinitionClass, expressionsToReplace, CalculateReplaceSpan, viewModel.StepText);

            viewModel.SelectedStepDefinitionProject.IdeScope.Logger.Log(TraceLevel.Info, method.AttributeLists.Count.ToString());

            return true;
        }

        private static MethodDeclarationSyntax? GetMethod(ProjectStepDefinitionBinding projectStepDefinitionBinding, 
            ITextBuffer textBuffer)
        {
            Document roslynDocument = textBuffer.GetRelatedDocuments().Single();
            
            var rootNode = roslynDocument.GetSyntaxRootAsync().Result;
            if (rootNode == null)
                return null;
            var methodLine =
                textBuffer.CurrentSnapshot.GetLineFromLineNumber(projectStepDefinitionBinding.Implementation
                    .SourceLocation.SourceFileLine - 1);
            var methodColumn = projectStepDefinitionBinding.Implementation.SourceLocation.SourceFileColumn - 1;
            var methodPosition = methodLine.Start + methodColumn;
            var node = rootNode.FindNode(new TextSpan(methodPosition, 1));
            var method = node.Parent as MethodDeclarationSyntax;

            return method;
        }

        private static SyntaxToken[] ExpressionsToReplace(RenameStepViewModel viewModel, MethodDeclarationSyntax method)
        {
            var attributesWithMatchingExpression = GetAttributesWithTokens(method)
                .Where(awt => !awt.Item2.IsMissing && MatchesWithOriginalText(awt.Item2))
                .ToArray();

            if (attributesWithMatchingExpression.Length > 1)
                attributesWithMatchingExpression =
                    attributesWithMatchingExpression
                        .Where(awt => MatchesAttributeNameWithStepType(awt.Item1))
                        .ToArray();

            var stepDefinitionAttributeTextTokens =
                attributesWithMatchingExpression
                    .Select(awt => awt.Item2)
                    .OrderByDescending(tok => tok.SpanStart)
                    .ToArray();

            return stepDefinitionAttributeTextTokens;

            bool MatchesWithOriginalText(SyntaxToken tok) => tok.ValueText == viewModel.OriginalStepText;
            bool MatchesAttributeNameWithStepType(AttributeSyntax a) => viewModel.SelectedStepDefinitionBinding.StepDefinitionType.ToString().Equals(a.Name.ToString());
        }

        internal static IEnumerable<Tuple<AttributeSyntax, SyntaxToken>> GetAttributesWithTokens(MethodDeclarationSyntax method)
        {
            return method.AttributeLists
                .SelectMany(al => al.Attributes)
                .Select(a => new Tuple<AttributeSyntax, SyntaxToken>(a, GetAttributeToken(a)));
        }

        private static SyntaxToken GetAttributeToken(AttributeSyntax attributeSyntax)
        {
            AttributeArgumentListSyntax? attributeArgumentListSyntax = attributeSyntax.ArgumentList;
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