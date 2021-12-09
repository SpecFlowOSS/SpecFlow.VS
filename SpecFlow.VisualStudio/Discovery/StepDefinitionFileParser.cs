﻿namespace SpecFlow.VisualStudio.Discovery;

public class StepDefinitionFileParser
{
    private readonly IDeveroomLogger _logger;

    public StepDefinitionFileParser(IDeveroomLogger logger)
    {
        _logger = logger;
    }

    public async Task<List<ProjectStepDefinitionBinding>> Parse(CSharpStepDefinitionFile stepDefinitionFile)
    {
        SyntaxTree tree = CSharpSyntaxTree.ParseText(stepDefinitionFile.Content);
        var rootNode = await tree.GetRootAsync();

        var allMethods = rootNode
            .DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .ToArray();

        var projectStepDefinitionBindings = new List<ProjectStepDefinitionBinding>(allMethods.Length);
        foreach (MethodDeclarationSyntax method in allMethods)
        {
            var attributes = RenameStepStepDefinitionClassAction.GetAttributesWithTokens(method);

            var methodBodyBeginToken = method.Body.GetFirstToken();
            var methodBodyBeginPosition = methodBodyBeginToken.GetLocation().GetLineSpan().StartLinePosition;
            var methodBodyEndToken = method.Body.GetLastToken();
            var methodBodyEndPosition = methodBodyEndToken.GetLocation().GetLineSpan().StartLinePosition;

            Scope scope = null;
            var parameterTypes = method.ParameterList.Parameters
                .Select(p => p.Type.ToString())
                .ToArray();

            var sourceLocation = new SourceLocation(stepDefinitionFile.StepDefinitionPath.FullName,
                methodBodyBeginPosition.Line + 1,
                methodBodyBeginPosition.Character + 1,
                methodBodyEndPosition.Line + 1,
                methodBodyEndPosition.Character + 1);
            var implementation =
                new ProjectStepDefinitionImplementation(FullMethodName(method), parameterTypes, sourceLocation);

            foreach (var (attribute, token) in attributes)
            {
                var stepDefinitionType = (ScenarioBlock)Enum.Parse(typeof(ScenarioBlock), attribute.Name.ToString());
                var regex = new Regex($"^{token.ValueText}$");

                var stepDefinitionBinding = new ProjectStepDefinitionBinding(stepDefinitionType, regex, scope,
                    implementation, token.ValueText);

                projectStepDefinitionBindings.Add(stepDefinitionBinding);
            }
        }

        _logger.LogVerbose($"Parse found {projectStepDefinitionBindings.Count} stepdefs");
        return projectStepDefinitionBindings;
    }

    private static string FullMethodName(MethodDeclarationSyntax method)
    {
        StringBuilder sb = new StringBuilder();
        var containingClass = method.Parent as ClassDeclarationSyntax;
        if (containingClass.Parent is BaseNamespaceDeclarationSyntax namespaceSyntax)
        {
            var containingNamespace = namespaceSyntax.Name;
            sb.Append(containingNamespace).Append('.');
        }

        sb.Append(containingClass.Identifier.Text).Append('.').Append(method.Identifier.Text);
        return sb.ToString();
    }
}