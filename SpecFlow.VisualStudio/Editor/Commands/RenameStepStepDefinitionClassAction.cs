#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using SpecFlow.VisualStudio.Diagnostics;

namespace SpecFlow.VisualStudio.Editor.Commands
{
    internal class RenameStepStepDefinitionClassAction : RenameStepAction
    {
        public override async Task PerformRenameStep(RenameStepCommandContext ctx)
        {
            GetMethod(ctx);
            if (ctx.IsErroneous) return;

            var expressionsToReplace = ExpressionsToReplace(ctx);
            if (ctx.IsErroneous) return;

            Func<SyntaxToken, string> replacementTextCalculation = UpdatedExpressionHasOperators() 
                ? FromExpressionWithoutOperators 
                : FromSimpleExpression;
            
            await EditTextBuffer(ctx.TextBufferOfStepDefinitionClass, ctx.IdeScope, expressionsToReplace, CalculateReplaceSpan, replacementTextCalculation);

            ctx.ProjectOfStepDefinitionClass.IdeScope.Logger.LogInfo(ctx.Method.AttributeLists.Count.ToString());

            bool UpdatedExpressionHasOperators() => ctx.UpdatedExpression.Contains('\\') || ctx.UpdatedExpression.Contains('"');
            string FromSimpleExpression(SyntaxToken token) => $"{(IsVerbatim(token)?"@":"")}\"{ctx.UpdatedExpression}\"";
            string FromExpressionWithoutOperators(SyntaxToken token) => $"@\"{ctx.UpdatedExpression.Replace("\"", "\"\"")}\"";
        }

        private void GetMethod(RenameStepCommandContext ctx)
        {
            var syntaxTree = ctx.IdeScope.GetSyntaxTree(ctx.TextBufferOfStepDefinitionClass);
            if (!syntaxTree.TryGetRoot(out SyntaxNode? rootNode)){
                ctx.AddCriticalProblem("Couldn't find syntax root");
                return;
            }

            var methodLine =
                ctx.TextBufferOfStepDefinitionClass.CurrentSnapshot.GetLineFromLineNumber(ctx.StepDefinitionBinding.Implementation
                    .SourceLocation.SourceFileLine - 1);
            var methodColumn = ctx.StepDefinitionBinding.Implementation.SourceLocation.SourceFileColumn - 1;
            var methodPosition = methodLine.Start + methodColumn;
            var node = rootNode.FindNode(new TextSpan(methodPosition, 1));

            ctx.Method = node.Parent as MethodDeclarationSyntax;
            if (ctx.Method == null)
            {
                ctx.AddCriticalProblem($"Method not found for {ctx.StepDefinitionBinding}.");
            }
        }

        private static SyntaxToken[] ExpressionsToReplace(RenameStepCommandContext ctx)
        {
            var attributesWithMatchingExpression = GetAttributesWithTokens(ctx.Method)
                .Where(awt => !awt.token.IsMissing && MatchesWithOriginalText(awt.token))
                .ToArray();

            if (attributesWithMatchingExpression.Length > 1)
                attributesWithMatchingExpression =
                    attributesWithMatchingExpression
                        .Where(awt => MatchesAttributeNameWithStepType(awt.attribute))
                        .ToArray();

            var stepDefinitionAttributeTextTokens =
                attributesWithMatchingExpression
                    .Select(awt => awt.token)
                    .OrderByDescending(tok => tok.SpanStart)
                    .ToArray();

            if (stepDefinitionAttributeTextTokens.Length == 0)
            {
                ctx.AddCriticalProblem($"No expressions found to replace for {ctx.StepDefinitionBinding}");
            }

            return stepDefinitionAttributeTextTokens;

            bool MatchesWithOriginalText(SyntaxToken tok) => tok.ValueText == ctx.OriginalExpression;
            bool MatchesAttributeNameWithStepType(AttributeSyntax a) => ctx.StepDefinitionBinding.StepDefinitionType.ToString().Equals(a.Name.ToString());
        }

        internal static IEnumerable<(AttributeSyntax attribute, SyntaxToken token)> GetAttributesWithTokens(MethodDeclarationSyntax method)
        {
            return method.AttributeLists
                .SelectMany(al => al.Attributes)
                .Select(a => (a, GetAttributeToken(a)));
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
            var replaceSpan = new Span(token.SpanStart, token.Span.Length);
            return replaceSpan;
        }

        private static bool IsVerbatim(SyntaxToken token)
        {
            return token.Text.StartsWith("@");
        }
    }
}