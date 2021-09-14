using System;
using System.Diagnostics;
using System.Linq;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.Text.Editor;
using SpecFlow.VisualStudio.Editor.Commands;
using SpecFlow.VisualStudio.VsxStubs;
using SpecFlow.VisualStudio.Diagnostics;
using SpecFlow.VisualStudio.UI.ViewModels;
using SpecFlow.VisualStudio.VsxStubs.ProjectSystem;
using SpecFlow.VisualStudio.VsxStubs.StepDefinitions;
using Xunit;
using Xunit.Abstractions;

namespace SpecFlow.VisualStudio.Tests.Editor.Commands
{
    public class RenameStepCommandTests
    {
        private readonly ITestOutputHelper _testOutputHelper;
        private readonly InMemoryStubProjectScope _projectScope;

        public RenameStepCommandTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
            StubIdeScope ideScope = new StubIdeScope(testOutputHelper);
            _projectScope = new InMemoryStubProjectScope(ideScope);
        }

        private StubWpfTextView CreateTextView(TestText inputText, string newLine = null)
        {
            return StubWpfTextView.CreateTextView(_projectScope.IdeScope as StubIdeScope, inputText, newLine, _projectScope, LanguageNames.CSharp, "Steps.cs");
        }

        private StubLogger GetStubLogger()
        {
            var stubLogger = (_projectScope.IdeScope.Logger as DeveroomCompositeLogger).Single(logger =>
                logger.GetType() == typeof(StubLogger)) as StubLogger;
            return stubLogger;
        }

        private static StubLogger GetStubLogger(StubIdeScope ideScope)
        {
            var stubLogger = (ideScope.Logger as DeveroomCompositeLogger).Single(logger =>
                logger.GetType() == typeof(StubLogger)) as StubLogger;
            return stubLogger;
        }

        private static bool NotSpecflowProject(Tuple<TraceLevel, string> msg)
        {
            return msg.Item2 == "ShowProblem: User Notification: Unable to find step definition usages: the project is not detected to be a SpecFlow project or it is not initialized yet.";
        }

        private static bool SpecflowProjectMustHaveFeatureFiles(Tuple<TraceLevel, string> msg)
        {
            return msg.Item2 == "ShowProblem: User Notification: Unable to find step definition usages: could not find any SpecFlow project with feature files.";
        }  
        
        private static TestFeatureFile[] ArrangeOneFeatureFile(string featureFileContent)
        {
            var featureFiles = new[]
            {
                new TestFeatureFile("calculator.feature", featureFileContent)
            };
            return featureFiles;
        }

        private static TestStepDefinition[] ArrangeOneStepDefinition(string textExpression)
        {
            var testStepDefinition = ArrangeStepDefinition(textExpression);
            var stepDefinitions = new[]
            {
                testStepDefinition
            };
            return stepDefinitions;
        }

        private static TestStepDefinition ArrangeStepDefinition(string textExpression, string keyWord = "When", string attributeName = null)
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

        private void ArrangePopup(string modelStepText)
        {
            (_projectScope.IdeScope.WindowManager as StubWindowManager)
                .RegisterWindowAction<RenameStepViewModel>(model => model.StepText = modelStepText);
        }

        private (StubWpfTextView textView, RenameStepCommand command) ArrangeSut(
            TestStepDefinition[] stepDefinitions,
            TestFeatureFile[] featureFiles)
        {
            var stepDefinitionClassFile = new StepDefinitionClassFile(stepDefinitions);
            var inputText = stepDefinitionClassFile.GetText();
            Dump(inputText, "Generated step definition class");

            _projectScope.AddSpecFlowPackage();
            foreach (var featureFile in featureFiles)
            {
                _projectScope.AddFile(featureFile.FileName, featureFile.Content);
            }

            var discoveryService =
                MockableDiscoveryService.SetupWithInitialStepDefinitions(_projectScope,
                    stepDefinitionClassFile.StepDefinitions);
            discoveryService.WaitUntilDiscoveryPerformed();

            var textView = CreateTextView(inputText);
            inputText.MoveCaretTo(textView, stepDefinitionClassFile.CaretPositionLine, stepDefinitionClassFile.CaretPositionColumn);

            var command = new RenameStepCommand(_projectScope.IdeScope, null, null);
            return (textView, command);
        }

        private TestText Dump(IWpfTextView textView, string title)
        {
            var testText = new TestText(textView.TextBuffer.CurrentSnapshot.Lines.Select(l => l.GetText()).ToArray());
            Dump(testText, title);
            return testText;
        }


        private void Dump(TestText inputText, string title)
        {
            _testOutputHelper.WriteLine($"-------{title}-------");
            _testOutputHelper.WriteLine(inputText.ToString());
            _testOutputHelper.WriteLine("---------------------------------------------");
        }

        [Fact]
        public void There_is_a_project_in_ide()
        {
            var emptyIde = new StubIdeScope(_testOutputHelper);
            var command = new RenameStepCommand(emptyIde, null, null);
            var inputText = new TestText(string.Empty);
            var textView = StubWpfTextView.CreateTextView(emptyIde, inputText);
            
            command.PreExec(textView, command.Targets.First());

            var stubLogger = GetStubLogger(emptyIde);
            stubLogger.Messages.Should().Contain(msg => NotSpecflowProject(msg));
        }

        [Fact]
        public void Only_specflow_projects_are_supported()
        {
            var command = new RenameStepCommand(_projectScope.IdeScope, null, null);
            var inputText = new TestText(string.Empty);
            var textView = CreateTextView(inputText);

            command.PreExec(textView, command.Targets.First());

            var stubLogger = GetStubLogger();
            stubLogger.Messages.Should().Contain(msg => NotSpecflowProject(msg));
        }

        [Fact]
        public void Specflow_projects_must_have_feature_files()
        {
            _projectScope.AddSpecFlowPackage();
            var command = new RenameStepCommand(_projectScope.IdeScope, null, null);
            var inputText = new TestText(string.Empty);

            var textView = CreateTextView(inputText);
            command.PreExec(textView, command.Targets.First());

            var stubLogger = GetStubLogger();
            stubLogger.Messages.Should().Contain(msg => SpecflowProjectMustHaveFeatureFiles(msg));
        }

        [Fact]
        public void There_must_be_at_lest_one_step_definition()
        {
            StepDefinitionMustHaveValidExpression(Array.Empty<TestStepDefinition>(), "ShowProblem: User Notification: No step definition found that is related to this position");
        }

        [Fact]
        public void StepDefinition_regex_must_be_valid()
        {
            var stepDefinitions = ArrangeOneStepDefinition(string.Empty);
            stepDefinitions[0].TestExpression = SyntaxFactory.MissingToken(SyntaxKind.StringLiteralToken);
            stepDefinitions[0].Regex = default;

            StepDefinitionMustHaveValidExpression(stepDefinitions, "ShowProblem: User Notification: Unable to rename step, the step definition expression cannot be detected.");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void StepDefinition_expression_cannot_be_modified(string emptyExpression)
        {
            var stepDefinitions = ArrangeOneStepDefinition(emptyExpression);

            StepDefinitionMustHaveValidExpression(stepDefinitions, "ShowProblem: User Notification: There was an error during step definition class rename.");
        }

        [Theory]
        [InlineData("\"\"")]
        public void StepDefinition_expression_must_be_valid(string invalidExpression)
        {
            var stepDefinitions = ArrangeOneStepDefinition(invalidExpression);

            StepDefinitionMustHaveValidExpression(stepDefinitions, "ShowProblem: User Notification: Step definition expression is invalid");
        }

        [Fact]
        public void Constant_is_not_supported_in_step_definition_expression()
        {
            var stepDefinitions = ArrangeOneStepDefinition("ConstantValue");
            stepDefinitions[0].Regex = "^I press add$";

            StepDefinitionMustHaveValidExpression(stepDefinitions, "ShowProblem: User Notification: There was an error during step definition class rename.");
        }

        private void StepDefinitionMustHaveValidExpression(TestStepDefinition[] stepDefinitions, string errorMessage)
        {
            var featureFiles = ArrangeOneFeatureFile(string.Empty);
            var (textView, command) = ArrangeSut(stepDefinitions, featureFiles);

            command.PreExec(textView, command.Targets.First());

            Dump(textView, "Step definition class after rename");
            var stubLogger = GetStubLogger();
            stubLogger.Messages.Last().Item2.Should().Be(errorMessage);
        }

        [Theory]
        [InlineData(1, @"""I press add""", @"I choose add", @"        [When(""I choose add"")]")]
        [InlineData(2, @"""I press add""", @"I \""choose\"" add", @"        [When(""I \""choose\"" add"")]")]
        [InlineData(3, @"""I \""press\"" add""", @"I choose add", @"        [When(""I choose add"")]")]
        [InlineData(4, @"""I \""press\"" add""", @"I \""choose\"" add", @"        [When(""I \""choose\"" add"")]")]
        [InlineData(5, @"""\""I press add \""""", @"\""I choose add\""", @"        [When(""\""I choose add\"""")]")]
        [InlineData(6, @"@""I press add""", @"I choose add", @"        [When(@""I choose add"")]")]
        [InlineData(7, @"@""I """"press"""" add""", @"I ""choose"" add", @"        [When(@""I ""choose"" add"")]")]
        public void Step_definition_class_has_one_matching_expression(int _, string testExpression, string modelStepText, string expectedLine)
        {
            var stepDefinitions = ArrangeOneStepDefinition(testExpression);
            var featureFiles = ArrangeOneFeatureFile(string.Empty);
            ArrangePopup(modelStepText);
            var (textView, command) = ArrangeSut(stepDefinitions, featureFiles);
            
            command.PreExec(textView, command.Targets.First());

            var testText = Dump(textView, "Step definition class after rename");
            testText.Lines[6].Should().Be(expectedLine);
        }

        [Fact]
        public void Step_definition_is_declared_with_a_derived_attribute()
        {
            var stepDefinition = ArrangeStepDefinition(@"""I press add""", attributeName: "WhenDerived");
            var featureFiles = ArrangeOneFeatureFile(string.Empty);
            ArrangePopup(@"I choose add");
            var (textView, command) = ArrangeSut(new[] { stepDefinition}, featureFiles);
            
            command.PreExec(textView, command.Targets.First());

            var testText = Dump(textView, "Step definition class after rename");
            testText.Lines[6].Should().Be(@"        [WhenDerived(""I choose add"")]");
        }
    }
}
