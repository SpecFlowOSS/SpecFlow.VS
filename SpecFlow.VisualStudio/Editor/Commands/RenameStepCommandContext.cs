using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualStudio.Text;
using SpecFlow.VisualStudio.Discovery;
using SpecFlow.VisualStudio.Editor.Services.StepDefinitions;
using SpecFlow.VisualStudio.ProjectSystem;

namespace SpecFlow.VisualStudio.Editor.Commands
{
    public class RenameStepCommandContext
    {
        public RenameStepCommandContext(IIdeScope ideScope)
        {
            IdeScope = ideScope;
            Issues = new List<Issue>();
        }

        public IIdeScope IdeScope { get;  }
        public List<Issue> Issues { get; }
        public ITextBuffer TextBufferOfStepDefinitionClass { get; set; }
        public IProjectScope ProjectOfStepDefinitionClass { get; set; }
        public IStepDefinitionExpressionAnalyzer StepDefinitionExpressionAnalyzer { get; set; }
        public MethodDeclarationSyntax Method { get; set; }

        public IProjectScope[] SpecFlowTestProjectsWithFeatureFiles { get; set; }
        public ProjectStepDefinitionBinding StepDefinitionBinding { get; set; }
        public IProjectScope StepDefinitionProjectScope { get; set; }

        public string OriginalExpression => StepDefinitionBinding.Expression;
        public AnalyzedStepDefinitionExpression AnalyzedOriginalExpression { get; set; }
        public string UpdatedExpression { get; set; }
        public AnalyzedStepDefinitionExpression AnalyzedUpdatedExpression { get; set; }
        public bool IsErroneous => Issues.Count != 0;

        public void AddProblem(string description)
        {
            AddIssue(Issue.IssueKind.Problem, description);
        }

        public void AddIssue(Issue.IssueKind kind, string description)
        {
            Issues.Add(new Issue(kind, description));
        }
    }
}