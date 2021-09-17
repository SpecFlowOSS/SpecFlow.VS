
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SpecFlow.VisualStudio.Discovery;
using SpecFlow.VisualStudio.VsxStubs;

namespace SpecFlow.VisualStudio.Tests.Editor.Commands
{
    public class StepDefinitionClassFile
    {
        public string MethodName { get; private set; }

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
            MethodName = "WhenIPressAdd";
            CaretPositionLine = 8;
            CaretPositionColumn = 9;
        }

        public string ContentTemplate {  get;  }
    
        public TestStepDefinition[] StepDefinitions { get; }

        public int CaretPositionLine { get; private set; }
        public int CaretPositionColumn { get; private set; }

        public TestText GetText(string filePath)
        {
            var insertLineNumber = 6;
            var testText = new TestText(ContentTemplate);
            var stepDefinitionText = new StringBuilder();
           
            List<string> lines = testText.Lines.Take(insertLineNumber).ToList();
            CaretPositionLine += StepDefinitions.Length;
            foreach (var stepDefinition in StepDefinitions)
            {
                MethodName = stepDefinition.Method;
                stepDefinition.TestSourceLocation = new SourceLocation(filePath, CaretPositionLine, CaretPositionColumn);
                Append(stepDefinitionText, stepDefinition);
            }
            AppendMethod(stepDefinitionText, MethodName);

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
            stepDefinitionText.Append(' ', 8).Append('[').Append(stepDefinition.AttributeName);
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