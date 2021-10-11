using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using SpecFlow.VisualStudio.Analytics;
using SpecFlow.VisualStudio.Discovery;
using SpecFlow.VisualStudio.Editor.Commands;
using SpecFlow.VisualStudio.Editor.Commands.Infrastructure;
using SpecFlow.VisualStudio.Editor.Services;
using SpecFlow.VisualStudio.ProjectSystem;
using SpecFlow.VisualStudio.VsxStubs;
using SpecFlow.VisualStudio.VsxStubs.ProjectSystem;
using SpecFlow.VisualStudio.VsxStubs.StepDefinitions;
using Xunit.Abstractions;

namespace SpecFlow.VisualStudio.Tests.Editor.Commands
{
    public abstract class CommandTestBase<T> where T: DeveroomEditorCommandBase
    {
        protected readonly ITestOutputHelper TestOutputHelper;
        protected readonly InMemoryStubProjectScope ProjectScope;
        private readonly Func<IProjectScope, T> _commandFactory;
        private readonly string _completedEventSignal;

        protected CommandTestBase(ITestOutputHelper testOutputHelper, Func<IProjectScope, T> commandFactory, string completedEventSignal)
        {
            TestOutputHelper = testOutputHelper;
            _commandFactory = commandFactory;
            StubIdeScope ideScope = new StubIdeScope(testOutputHelper);

            ProjectScope = new InMemoryStubProjectScope(ideScope);
            _completedEventSignal = completedEventSignal;
        }

        protected (StubWpfTextView textView, T command) ArrangeSut(
            TestStepDefinition stepDefinition, TestFeatureFile featureFile)
        {
            var stepDefinitions = stepDefinition.IsVoid
                ? Array.Empty<TestStepDefinition>()
                : new[] { stepDefinition };

            var featureFiles = featureFile.IsVoid
                ? Array.Empty<TestFeatureFile>()
                : new[] { featureFile };

            return ArrangeSut(stepDefinitions, featureFiles);
        }

        protected (StubWpfTextView textView, T command) ArrangeSut(
            TestStepDefinition[] stepDefinitions,
            TestFeatureFile[] featureFiles)
        {
            var textView = ArrangeTextView(stepDefinitions, featureFiles);

            var command = _commandFactory(ProjectScope);
            return (textView, command);
        }

        private StubWpfTextView ArrangeTextView(
            TestStepDefinition[] stepDefinitions,
            TestFeatureFile[] featureFiles)
        {
            var stepDefinitionClassFile = new StepDefinitionClassFile(stepDefinitions);
            var filePath = Path.Combine(ProjectScope.ProjectFolder, "Steps.cs");
            var inputText = stepDefinitionClassFile.GetText(filePath);
            Dump(inputText.ToString(), "Generated step definition class");

            ProjectScope.AddSpecFlowPackage();
            foreach (var featureFile in featureFiles)
            {
                ProjectScope.AddFile(featureFile.FileName, featureFile.Content);
            }

            var discoveryService =
                MockableDiscoveryService.SetupWithInitialStepDefinitions(ProjectScope, stepDefinitionClassFile.StepDefinitions, TimeSpan.Zero);
            discoveryService.WaitUntilDiscoveryPerformed();

            var textView = CreateTextView(inputText);
            inputText.MoveCaretTo(textView, stepDefinitionClassFile.CaretPositionLine, stepDefinitionClassFile.CaretPositionColumn);

            return textView;
        }

        protected TestText Dump(TestFeatureFile featureFile, string title)
        {
            ProjectScope.IdeScope.GetTextBuffer(new SourceLocation(featureFile.FileName, 1, 1), out var featureFileTextBuffer);
            return Dump(featureFileTextBuffer, title);
        }

        protected TestText Dump(IWpfTextView textView, string title)
        {
            return Dump(textView.TextBuffer, title);
        }

        protected TestText Dump(ITextBuffer textBuffer, string title)
        {
            var testText = new TestText(textBuffer.CurrentSnapshot.Lines.Select(l => l.GetText()).ToArray());
            Dump(testText.ToString(), title);
            return testText;
        }

        protected void Dump(string content, string title)
        {
            TestOutputHelper.WriteLine($"-------{title}-------");
            TestOutputHelper.WriteLine(content);
            TestOutputHelper.WriteLine("---------------------------------------------");
        }

        protected StubWpfTextView CreateTextView(TestText inputText, string newLine = null)
        {
            return ProjectScope.StubIdeScope.CreateTextView(
                inputText,
                newLine,
                ProjectScope,
                VsContentTypes.CSharp,
                "Steps.cs");
        }

        protected TestFeatureFile ArrangeOneFeatureFile()
        {
            return ArrangeOneFeatureFile(
$@"Feature: Feature1
   Scenario: Scenario1
       When I press add");
        }

        protected TestFeatureFile ArrangeOneFeatureFile(string featureFileContent)
        {
            var filePath = Path.Combine(ProjectScope.ProjectFolder, "calculator.feature");
            var featureFile = new TestFeatureFile(filePath, featureFileContent);
            if (!featureFile.IsVoid)
            {
                ProjectScope.IdeScope.FileSystem.Directory.CreateDirectory(ProjectScope.ProjectFolder);
                ProjectScope.IdeScope.FileSystem.File.WriteAllText(featureFile.FileName, featureFileContent);
            }

            if (string.IsNullOrWhiteSpace(featureFileContent)) return featureFile;

            Dump(featureFile.Content, "Arranged feature file");

            return featureFile;
        }

        protected static TestStepDefinition[] ArrangeMultipleStepDefinitions()
        {
            var stepDefinitions = new[]
            {
                ArrangeStepDefinition(@"""I press add""", "When"),
                ArrangeStepDefinition(@"""I press add""", "Given"),
                ArrangeStepDefinition(@"""I select add""", "When")
            };
            return stepDefinitions;
        }

        protected static TestStepDefinition ArrangeStepDefinition(string textExpression, string keyWord = "When", string attributeName = null)
        {
            var token =  textExpression==null
                ? SyntaxFactory.MissingToken(SyntaxKind.StringLiteralToken)
                : SyntaxFactory.ParseToken(textExpression);
            var testStepDefinition = new TestStepDefinition
            {
                Method = keyWord + "IPressAdd",
                Type = keyWord,
                TestExpression = token,
                AttributeName = attributeName ?? keyWord
            };
            return testStepDefinition;
        }

        protected Task<IAnalyticsEvent> Invoke(T command, StubWpfTextView textView)
        {
            command.PreExec(textView, command.Targets.First());
            return WaitForCommandToComplete();
        }

        protected Task<IAnalyticsEvent> WaitForCommandToComplete()
        {
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
            return ProjectScope.StubIdeScope.AnalyticsTransmitter
                .WaitForEventAsync(_completedEventSignal, cts.Token);
        }

        protected void ModifyFeatureFileInEditor(TestFeatureFile featureFile, Span span, string replacementText)
        {
            var sl = new SourceLocation(featureFile.FileName, 1, 1);
            ProjectScope.IdeScope.OpenIfNotOpened(featureFile.FileName);
            ProjectScope.IdeScope.GetTextBuffer(sl, out var textBuffer);

            using var textEdit = textBuffer.CreateEdit();
            textEdit.Replace(span, replacementText);

            textEdit.Apply();
        }
    }
}
