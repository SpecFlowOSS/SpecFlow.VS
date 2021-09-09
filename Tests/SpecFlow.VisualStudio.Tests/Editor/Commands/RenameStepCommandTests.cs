using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Text;
using SpecFlow.VisualStudio.Editor.Commands;
using SpecFlow.VisualStudio.VsxStubs;
using SpecFlow.VisualStudio.Diagnostics;
using SpecFlow.VisualStudio.ProjectSystem.Settings;
using SpecFlow.VisualStudio.SpecFlowConnector.Models;
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
        private static bool MissingStepDefinition(Tuple<TraceLevel, string> msg)
        {
            return msg.Item2 == "ShowProblem: User Notification: No step definition found that is related to this position";
        }   
        
        private static bool MissingStepDefinitionExpression(Tuple<TraceLevel, string> msg)
        {
            return msg.Item2 == "ShowProblem: User Notification: Unable to rename step, the step definition expression cannot be detected.";
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
            _projectScope.AddSpecFlowPackage();
            _projectScope.AddFile("calculator.feature", string.Empty);
            var command = new RenameStepCommand(_projectScope.IdeScope, null, null);
            var inputText = new TestText(string.Empty);

            var textView = CreateTextView(inputText);
            command.PreExec(textView, command.Targets.First());

            var stubLogger = GetStubLogger();
            stubLogger.Messages.Should().Contain(msg => MissingStepDefinition(msg));
        }

        [Fact]
        public void StepDefinition_must_have_expression()
        {
            var stepDefinitions = new[]
            {
                new StepDefinition
                {
                    Method = "WhenIPressAdd",
                    Type = "When",
                    //Regex = "^I press add$",
                    SourceLocation = @"Steps.cs|1|10"
                }
            };

            _projectScope.AddSpecFlowPackage();
            _projectScope.AddFile("calculator.feature", string.Empty);

            var discoveryService = MockableDiscoveryService.SetupWithInitialStepDefinitions(_projectScope, stepDefinitions);
            discoveryService.WaitUntilDiscoveryPerformed();

            var command = new RenameStepCommand(_projectScope.IdeScope, null, null);
            var inputText = new TestText(string.Empty);

            var textView = CreateTextView(inputText);
            textView.TextBuffer.Insert(0, "[When(\"I press add\")]");
            textView.Caret.MoveTo(new SnapshotPoint(textView.TextSnapshot, 10));
            command.PreExec(textView, command.Targets.First());

            var stubLogger = GetStubLogger();
            stubLogger.Messages.Should().Contain(msg => MissingStepDefinitionExpression(msg));
        }
    }
}
