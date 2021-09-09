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
            ContentTemplate = @"
using System;
using TechTalk.SpecFlow;
namespace MyProject
{
}
";
        }

        public string ContentTemplate {  get;  }
    
        public TestStepDefinition[] StepDefinitions { get; }

        public TestText GetText()
        {
            var insertLineNumber = 5;
            var testText = new TestText(ContentTemplate);
            var stepDefinitionText = new StringBuilder();

            List<string> lines = testText.Lines.Take(insertLineNumber).ToList();
            foreach (var stepDefinition in StepDefinitions)
            {
                var stepLines = Append(stepDefinitionText, stepDefinition);
                lines.AddRange(stepLines);
            }

            lines.AddRange(testText.Lines.Skip(insertLineNumber));

            return new TestText(lines.ToArray());
        }

        private static string[] Append(StringBuilder stepDefinitionText, TestStepDefinition stepDefinition)
        {
            stepDefinitionText.Append(' ', 8).Append('[').Append(stepDefinition.Type).Append("(\"")
                .Append(stepDefinition.TestExpression).AppendLine("\")]");

            stepDefinitionText.Append(' ', 8).Append("public void ").Append(stepDefinition.Method).AppendLine("()");

            stepDefinitionText.Append(' ', 8).AppendLine("{");
            stepDefinitionText.Append(' ', 8).AppendLine("}");

            return stepDefinitionText.ToString().Split(new[] { Environment.NewLine }, StringSplitOptions.None);
        }
    }
}