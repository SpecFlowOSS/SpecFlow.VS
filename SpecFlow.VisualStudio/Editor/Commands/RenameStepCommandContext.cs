using System.Collections.Generic;
using System.Linq;
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
            Issues = new List<Problem>();
        }

        public SnapshotPoint TriggerPointOfStepDefinitionClass { get; set; }
        public IIdeScope IdeScope { get;  }
        public List<Problem> Issues { get; }
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
        public bool IsErroneous => Issues.Any(issue=>issue.Kind == Problem.ProblemKind.Critical);

        public void AddCriticalProblem(string description)
        {
            AddProblem(Problem.ProblemKind.Critical, description);
        }

        public void AddNotificationProblem(string description)
        {
            AddProblem(Problem.ProblemKind.Notification, description);
        }

        public void AddProblem(Problem.ProblemKind kind, string description)
        {
            Issues.Add(new Problem(kind, description));
        }
    }
}