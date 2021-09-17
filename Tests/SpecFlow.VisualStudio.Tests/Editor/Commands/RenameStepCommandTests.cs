using System;
using System.Diagnostics;
using System.Linq;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using SpecFlow.VisualStudio.Editor.Commands;
using SpecFlow.VisualStudio.VsxStubs;
using SpecFlow.VisualStudio.Diagnostics;
using SpecFlow.VisualStudio.Discovery;
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

        private static bool SpecflowProjectMustHaveFeatureFiles(Tuple<TraceLevel, string> msg)
        {
            return msg.Item2 == "ShowProblem: User Notification: Unable to find step definition usages: could not find any SpecFlow project with feature files.";
        }  
        
        private TestFeatureFile ArrangeOneFeatureFile(string featureFileContent)
        {
            var featureFile = new TestFeatureFile("calculator.feature", featureFileContent);
            if (!featureFile.IsVoid)
                _projectScope.IdeScope.FileSystem.File.WriteAllText(featureFile.FileName, featureFileContent);
            return featureFile;
        }

        private static TestStepDefinition[] ArrangeMultipleStepDefinitions()
        {
            var stepDefinitions = new[]
            {
                ArrangeStepDefinition(@"""I press add""", "When"),
                ArrangeStepDefinition(@"""I press add""", "Given"),
                ArrangeStepDefinition(@"""I select add""", "When")
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
            TestStepDefinition stepDefinition, TestFeatureFile featureFile)
        {
            var stepDefinitions = stepDefinition.IsVoid
                ? Array.Empty<TestStepDefinition>() 
                : new[] {stepDefinition};

            var featureFiles = featureFile.IsVoid
                ? Array.Empty<TestFeatureFile>()
                : new[] { featureFile };

            return ArrangeSut(stepDefinitions, featureFiles);
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
            return Dump(textView.TextBuffer, title);
        }

        private TestText Dump(ITextBuffer textBuffer, string title)
        {
            var testText = new TestText(textBuffer.CurrentSnapshot.Lines.Select(l => l.GetText()).ToArray());
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
            stubLogger.Messages.Last().Item2.Should().Be("ShowProblem: User Notification: Unable to find step definition usages: the project is not initialized yet.");
        }

        [Fact]
        public void Only_specflow_projects_are_supported()
        {
            var command = new RenameStepCommand(_projectScope.IdeScope, null, null);
            var inputText = new TestText(string.Empty);
            var textView = CreateTextView(inputText);

            command.PreExec(textView, command.Targets.First());

            var stubLogger = GetStubLogger();
            stubLogger.Messages.Last().Item2.Should().Be("ShowProblem: User Notification: Unable to find step definition usages: the project is not detected to be a SpecFlow project.");
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
            StepDefinitionMustHaveValidExpression(TestStepDefinition.Void, "ShowProblem: User Notification: No step definition found that is related to this position");
        }

        [Fact]
        public void StepDefinition_regex_must_be_valid()
        {
            var stepDefinition = ArrangeStepDefinition(string.Empty);
            stepDefinition.TestExpression = SyntaxFactory.MissingToken(SyntaxKind.StringLiteralToken);
            stepDefinition.Regex = default;

            StepDefinitionMustHaveValidExpression(stepDefinition, "ShowProblem: User Notification: Unable to rename step, the step definition expression cannot be detected.");
        }

        [Theory]
        [InlineData(1, null)]
        [InlineData(2, "")]
        [InlineData(3, @"""foo? (\d+) bar""")]
        [InlineData(4, @"""foo (?:\d+) bar""")]
        [InlineData(5, @"""foo [a-z] bar""")]
        [InlineData(6, @"""foo. (\d+) bar""")]
        [InlineData(7, @"""foo* (\d+) bar""")]
        [InlineData(8, @"""foo+ (\d+) bar""")]
        // [InlineData(9, @"""some \\(context\\)""")] -- represents `some \(context\)` that is a simple part (all op masked)
        [InlineData(10, @"""some \{context\}""")]
        [InlineData(11, @"""some \[context\]""")]
        [InlineData(12, @"""some \[context]""")]
        // [InlineData(13, @"""chars \\\\\\*\\+\\?\\|\\{\\}\\[\\]\\(\\)\\^\\$\\#""")] -- represents `chars \\\*\+\?\|\{\}\[\]\(\)\^\$\#` that is a simple part (all op masked)
        public void StepDefinition_expression_cannot_be_modified(int _, string emptyExpression)
        {
            var stepDefinition = ArrangeStepDefinition(emptyExpression);

            StepDefinitionMustHaveValidExpression(stepDefinition, "ShowProblem: User Notification: The non-parameter parts cannot contain expression operators");
        }

        [Theory]
        [InlineData("\"\"")]
        public void StepDefinition_expression_must_be_valid(string invalidExpression)
        {
            var stepDefinition = ArrangeStepDefinition(invalidExpression);

            StepDefinitionMustHaveValidExpression(stepDefinition, "ShowProblem: User Notification: Step definition expression is invalid");
        }

        [Fact]
        public void Constant_is_not_supported_in_step_definition_expression()
        {
            var stepDefinition = ArrangeStepDefinition("ConstantValue");
            stepDefinition.Regex = "^I press add$";

            StepDefinitionMustHaveValidExpression(stepDefinition, "ShowProblem: User Notification: No expressions found to replace for [When(I press add)]: WhenIPressAdd");
        }

        private void StepDefinitionMustHaveValidExpression(TestStepDefinition stepDefinition, string errorMessage)
        {
            var featureFile = ArrangeOneFeatureFile(string.Empty);
            var (textView, command) = ArrangeSut(stepDefinition, featureFile);

            command.PreExec(textView, command.Targets.First());

            Dump(textView, "Step definition class after rename");
            var stubLogger = GetStubLogger();
            stubLogger.Messages.Last().Item2.Should().Be(errorMessage);
        }

        [Theory]
        [InlineData("01", @"""I press add""", @"I choose add", @"        [When(""I choose add"")]")]
        [InlineData("02", @"""I press add""", @"I \""choose\"" add", @"        [When(@""I \""""choose\"""" add"")]")]
        [InlineData("03", @"""I \""press\"" add""", @"I choose add", @"        [When(""I choose add"")]")]
        [InlineData("04", @"""I \""press\"" add""", @"I \""choose\"" add", @"        [When(@""I \""""choose\"""" add"")]")]
        [InlineData("05", @"""\""I press add \""""", @"\""I choose add\""", @"        [When(@""\""""I choose add\"""""")]")]
        [InlineData("06", @"@""I press add""", @"I choose add", @"        [When(@""I choose add"")]")]
        [InlineData("07", @"@""I """"press"""" add""", @"I ""choose"" add", @"        [When(@""I """"choose"""" add"")]")]
        [InlineData("08", @"""I press (/d)""", @"I choose (/d)", @"        [When(""I choose (/d)"")]")]
        [InlineData("09", @"""I press (.*)""", @"I press (.*) button", @"        [When(""I press (.*) button"")]")]
        [InlineData("10", @"""(.*) press add""", @"(.*) press add button", @"        [When(""(.*) press add button"")]")]
        [InlineData("11", @"""(.*) press add""", @"On main screen (.*) press add", @"        [When(""On main screen (.*) press add"")]")]
        [InlineData("12", @"""(.*) press (.*)""", @"(.*) choose (.*)", @"        [When(""(.*) choose (.*)"")]")]
        [InlineData("13", @"""I press (.*)(.*)""", @"I press (.*) and (.*)", @"        [When(""I press (.*) and (.*)"")]")]
        [InlineData("14", @"""I press add""", @"I choose \(add\)", @"        [When(@""I choose \(add\)"")]")]
        [InlineData("15", @"@""I press add""", @"I choose \(add\)", @"        [When(@""I choose \(add\)"")]")]
        [InlineData("16", @"@""I press add""", @"I choose ""add""", @"        [When(@""I choose """"add"""""")]")]
        [InlineData("17", @"""I press add""", @"I choose ""add""", @"        [When(@""I choose """"add"""""")]")]
        public void Step_definition_class_has_one_matching_expression(string _, string testExpression, string modelStepText, string expectedLine)
        {
            var stepDefinitions = ArrangeStepDefinition(testExpression);
            var featureFile = ArrangeOneFeatureFile(string.Empty);
            ArrangePopup(modelStepText);
            var (textView, command) = ArrangeSut(stepDefinitions, featureFile);
            
            command.PreExec(textView, command.Targets.First());

            var testText = Dump(textView, "Step definition class after rename");
            testText.Lines[6].Should().Be(expectedLine);
        }
        
        [Theory]
        [InlineData("01", @"""I press add""", @"I press (.*)", "Parameter count mismatch")]
        [InlineData("02", @"""I press (.*)""", @"I press add", "Parameter count mismatch")]
        [InlineData("03", @"""I press add button""", @"I press (.*) button", "Parameter count mismatch")]
        [InlineData("04", @"""I press (.*) button""", @"I press add button", "Parameter count mismatch")]
        [InlineData("05", @"""I press add button""", @"(.*) press add button", "Parameter count mismatch")]
        [InlineData("06", @"""(.*) press add button""", @"I press add button", "Parameter count mismatch")]
        [InlineData("07", @"""(.*) press add""", @"(.*) press (.*)", "Parameter count mismatch")]
        [InlineData("08", @"""(.*) press (.*)""", @"(.*) press add", "Parameter count mismatch")]
        [InlineData("09", @"""I press (.*)""", @"I press (.*)(.*)", "Parameter count mismatch")]
        [InlineData("10", @"""I press (.*)""", @"I press (/d)", "Parameter expression mismatch")]
        [InlineData("11", @"""I press (.*)""", @"I press (/d)(.*)", "Parameter count mismatch", "Parameter expression mismatch")]
        [InlineData("13", @"""I press add""", @"I ( add (.*)", "Parameter count mismatch")]
        [InlineData("14", @"""I press (.*)""", @"I press add .*)", "The non-parameter parts cannot contain expression operators", "Parameter count mismatch")]
        [InlineData("15", @"""I press add""", @"I pr?ess add", "The non-parameter parts cannot contain expression operators")]
        [InlineData("16", @"""I press (.*)""", @"I pr?ess (.*)", "The non-parameter parts cannot contain expression operators")]
        public void User_cannot_type_invalid_expression(string _, string testExpression, string modelStepText, params string[] errorMessages)
        {
            var stepDefinitions = ArrangeStepDefinition(testExpression);
            var featureFile = ArrangeOneFeatureFile(string.Empty);
            ArrangePopup(modelStepText);
            var (textView, command) = ArrangeSut(stepDefinitions, featureFile);

            command.PreExec(textView, command.Targets.First());

            var stubLogger = GetStubLogger();
            stubLogger.Messages.Last().Item2.Should().Be("ShowProblem: User Notification: " + string.Join(Environment.NewLine, errorMessages));
        }

        [Fact]
        public void Step_definition_is_declared_with_a_derived_attribute()
        {
            var stepDefinition = ArrangeStepDefinition(@"""I press add""", attributeName: "WhenDerived");
            var featureFile = ArrangeOneFeatureFile(string.Empty);
            ArrangePopup(@"I choose add");
            var (textView, command) = ArrangeSut(stepDefinition, featureFile);
            
            command.PreExec(textView, command.Targets.First());

            var testText = Dump(textView, "Step definition class after rename");
            testText.Lines[6].Should().Be(@"        [WhenDerived(""I choose add"")]");
        }
        
        [Fact]
        public void Popup_appears_when_there_are_multiple_step_definitions()
        {
            var stepDefinitions = ArrangeMultipleStepDefinitions();
            var featureFiles = new[] {ArrangeOneFeatureFile(string.Empty)};
            var (textView, command) = ArrangeSut( stepDefinitions, featureFiles);

            command.PreExec(textView, command.Targets.First());

            var ideActions = _projectScope.IdeScope.Actions as StubIdeActions;
            ideActions.LastShowContextMenuHeader.Should().Be("Choose step definition to rename");
            ideActions.LastShowContextMenuItems.Select(item => item.Label)
                .Should().BeEquivalentTo(stepDefinitions.Select(sd=>sd.PopupLabel));
        }

        [Theory]
        [InlineData(0, new [] {
            @"        [When(""I choose add"")]",
            @"        [Given(""I press add"")]",
            @"        [When(""I select add"")]" })]
        [InlineData(1, new [] {
            @"        [When(""I press add"")]",
            @"        [Given(""I choose add"")]",
            @"        [When(""I select add"")]" })]
        [InlineData(2, new [] {
            @"        [When(""I press add"")]",
            @"        [Given(""I press add"")]",
            @"        [When(""I choose add"")]" })]
        public void Selected_step_definition_is_renamed_when_there_are_multiple(int chosenOption, string[] expectedLines)
        {
            var stepDefinitions = ArrangeMultipleStepDefinitions();
            var featureFiles = new[] { ArrangeOneFeatureFile(string.Empty) };
            ArrangePopup(@"I choose add");
            var (textView, command) = ArrangeSut(stepDefinitions, featureFiles);

            command.PreExec(textView, command.Targets.First());
            var ideActions = _projectScope.IdeScope.Actions as StubIdeActions;
            var chosenItem = ideActions.LastShowContextMenuItems[chosenOption];
            chosenItem.Command(chosenItem);

            var testText = Dump(textView, "Step definition class after rename");
            testText.Lines[6].Should().Be(expectedLines[0]);
            testText.Lines[7].Should().Be(expectedLines[1]);
            testText.Lines[8].Should().Be(expectedLines[2]);
        }

        [Fact]
        public void Step_in_the_feature_file_is_renamed_simple_case()
        {
            var stepDefinition = ArrangeStepDefinition(@"""I press add""");
            var featureFile = ArrangeOneFeatureFile(@"Feature: Feature1
                Scenario: Scenario1
                    When I press add");
            ArrangePopup(@"I choose add");
            var (textView, command) = ArrangeSut(stepDefinition, featureFile);

            command.PreExec(textView, command.Targets.First());

            var featureFileTextBuffer = _projectScope.IdeScope.GetTextBuffer(new SourceLocation(featureFile.FileName, 1, 1));
            var featureText = Dump(featureFileTextBuffer, "Feature file after rename");
            featureText.Lines[2].Should().Be(@"                    When I choose add");
        }

        [Theory]
        [InlineData(1, @"""I press add""", @"I choose add", @"I press add", @"I choose add")]
        [InlineData(2, @"""I press add""", @"I choose \(add\)", @"I press add", @"I choose (add)")]
        [InlineData(3, @"""I press add (.*)""", @"I choose \(add\) (.*)", @"I press add 42", @"I choose (add) 42")]
        public void Step_in_the_feature_file_is_renamed(int _, string originalExpression, string updatedExpression, string originalStepText, string expectedStepText)
        {
            var stepDefinition = ArrangeStepDefinition(originalExpression);
            var featureFile = ArrangeOneFeatureFile($@"Feature: Feature1
                Scenario: Scenario1
                    When {originalStepText}");
            ArrangePopup(updatedExpression);
            var (textView, command) = ArrangeSut(stepDefinition, featureFile);

            command.PreExec(textView, command.Targets.First());

            var featureFileTextBuffer = _projectScope.IdeScope.GetTextBuffer(new SourceLocation(featureFile.FileName, 1, 1));
            var featureText = Dump(featureFileTextBuffer, "Feature file after rename");
            featureText.Lines[2].Should().Be($@"                    When {expectedStepText}");
        }

        [Theory]
        [InlineData(1, @"""I press add""", @"I choose add", @"I press add", @"I choose add")]
        [InlineData(2, @"""I press add""", @"I choose \(add\)", @"I press add", @"I choose (add)")]
        [InlineData(3, @"""I press add (.*)""", @"I choose \(add\) (.*)", @"I press add 42", @"I choose (add) 42")]
        [InlineData(4, @"""I press add""", @"I choose add", @"I press <p1>", @"I press <p1>")]
        [InlineData(5, @"""I press add (.*)""", @"I choose \(add\) (.*)", @"I press <p1> 42", @"I press <p1> 42")]
        public void Step_of_scenario_outline_in_the_feature_file_is_renamed(int _, string originalExpression, string updatedExpression, string originalStepText, string expectedStepText)
        {
            var stepDefinition = ArrangeStepDefinition(originalExpression);
            var featureFile = ArrangeOneFeatureFile($@"Feature: Feature1
                Scenario Outline: Scenario1
                    When {originalStepText}
                Examples:
                    |p1 |
                    |add|");
            ArrangePopup(updatedExpression);
            var (textView, command) = ArrangeSut(stepDefinition, featureFile);

            command.PreExec(textView, command.Targets.First());

            var featureFileTextBuffer = _projectScope.IdeScope.GetTextBuffer(new SourceLocation(featureFile.FileName, 1, 1));
            var featureText = Dump(featureFileTextBuffer, "Feature file after rename");
            featureText.Lines[2].Should().Be($@"                    When {expectedStepText}");
        }

        [Theory]
        [InlineData(1, @"""I press add""", @"I press add")]
        [InlineData(2, @"@""I press add""", @"I press add")]
        [InlineData(3, @"""I press \\(add\\)""", @"I press \(add\)")]
        [InlineData(4, @"@""I press \(add\)""", @"I press \(add\)")]
        public void The_right_expression_is_loaded_to_the_dialog(int _, string originalCSharpExpression, string expectedExpression)
        {
            var stepDefinition = ArrangeStepDefinition(originalCSharpExpression);
            var featureFile = ArrangeOneFeatureFile(string.Empty);
            RenameStepViewModel viewModel = null;
            (_projectScope.IdeScope.WindowManager as StubWindowManager)?
                .RegisterWindowAction<RenameStepViewModel>(model => viewModel = model);
            var (textView, command) = ArrangeSut(stepDefinition, featureFile);

            command.PreExec(textView, command.Targets.First());

            viewModel?.StepText.Should().Be(expectedExpression);
        }
    }
}
