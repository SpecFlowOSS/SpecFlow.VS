using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SpecFlow.VisualStudio.VsxStubs;

namespace SpecFlow.VisualStudio.Tests.Editor.Commands
{
    public class StepDefinitionClassFile
    {
        public StepDefinitionClassFile(TestStepDefinition[] stepDefinitions)
        {
            StepDefinitions = stepDefinitions;
            ContentTemplate = @"using System;
using TechTalk.SpecFlow;
namespace MyProject
{
    [Binding]
    public class Steps {
    }
}";
        }

        public string ContentTemplate {  get;  }
    
        public TestStepDefinition[] StepDefinitions { get; }

        public TestText GetText()
        {
            var insertLineNumber = 6;
            var testText = new TestText(ContentTemplate);
            var stepDefinitionText = new StringBuilder();

            List<string> lines = testText.Lines.Take(insertLineNumber).ToList();
            foreach (var stepDefinition in StepDefinitions)
            {
                Append(stepDefinitionText, stepDefinition);
            }
            AppendMethod(stepDefinitionText, StepDefinitions.First().Method);

            lines.AddRange(ToLines(stepDefinitionText));

            lines.AddRange(testText.Lines.Skip(insertLineNumber));

            return new TestText(lines.ToArray());
        }

        private static string[] ToLines(StringBuilder stepDefinitionText)
        {
            return stepDefinitionText.ToString().Split(new[] {Environment.NewLine}, StringSplitOptions.None);
        }

        private static void Append(StringBuilder stepDefinitionText, TestStepDefinition stepDefinition)
        {
            stepDefinitionText.Append(' ', 8).Append('[').Append(stepDefinition.Type);
            if (!stepDefinition.TestExpression.IsMissing)
                stepDefinitionText.Append("(").Append(stepDefinition.TestExpression.Text).Append(")");
            stepDefinitionText.AppendLine("]");
        }

        private static void AppendMethod(StringBuilder stepDefinitionText, string method)
        {
            stepDefinitionText.Append(' ', 8).Append("public void ").Append(method).AppendLine("()");

            stepDefinitionText.Append(' ', 8).AppendLine("{");
            stepDefinitionText.Append(' ', 8).AppendLine("}");
        }
    }
}