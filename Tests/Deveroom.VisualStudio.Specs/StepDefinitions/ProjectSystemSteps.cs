using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Deveroom.VisualStudio.Configuration;
using Deveroom.VisualStudio.Diagonostics;
using Deveroom.VisualStudio.Discovery;
using Deveroom.VisualStudio.Editor.Commands;
using Deveroom.VisualStudio.Editor.Commands.Infrastructure;
using Deveroom.VisualStudio.Editor.Completions;
using Deveroom.VisualStudio.Editor.Completions.Infrastructure;
using Deveroom.VisualStudio.Editor.Services;
using Deveroom.VisualStudio.Editor.Traceability;
using Deveroom.VisualStudio.ProjectSystem.Configuration;
using Deveroom.VisualStudio.SpecFlowConnector.Models;
using Deveroom.VisualStudio.Specs.Support;
using Deveroom.VisualStudio.UI.ViewModels;
using Deveroom.VisualStudio.VsxStubs;
using Deveroom.VisualStudio.VsxStubs.ProjectSystem;
using FluentAssertions;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using TechTalk.SpecFlow;
using TechTalk.SpecFlow.Assist;
using Xunit;
using ScenarioBlock = Deveroom.VisualStudio.Editor.Services.Parser.ScenarioBlock;

namespace Deveroom.VisualStudio.Specs.StepDefinitions
{
    [Binding]
    public class ProjectSystemSteps : Steps
    {
        private readonly StubIdeScope _ideScope;
        private InMemoryStubProjectScope _projectScope;
        private StubWpfTextView _wpfTextView;
        private MockableDiscoveryService _discoveryService;
        private ProjectStepDefinitionBinding _stepDefinitionBinding;
        private string _commandToInvokeDeferred = null;
        private StubCompletionBroker _completionBroker = null;

        private StubIdeActions ActionsMock => (StubIdeActions)_ideScope.Actions;

        public ProjectSystemSteps(StubIdeScope stubIdeScope)
        {
            _ideScope = stubIdeScope;
        }

        [Given(@"there is a SpecFlow project scope")]
        public void GivenThereIsASpecFlowProjectScope()
        {
            _projectScope = new InMemoryStubProjectScope(_ideScope);
            _projectScope.AddSpecFlowPackage();
            _discoveryService = MockableDiscoveryService.Setup(_projectScope);
        }

        [Given(@"there is a SpecFlow project scope with calculator step definitions")]
        public void GivenThereIsASpecFlowProjectScopeWithCalculatorStepDefinitions()
        {
            GivenThereIsASpecFlowProjectScope();

            _discoveryService.LastDiscoveryResult.StepDefinitions = new[]
            {
                new StepDefinition
                {
                    Method = "GivenIHaveEnteredIntoTheCalculator",
                    ParamTypes = "i",
                    Type = "Given",
                    Regex = "^I have entered (.*) into the calculator$",
                    SourceLocation = @"X:\ProjectMock\CalculatorSteps.cs|24|5"
                },
                new StepDefinition
                {
                    Method = "WhenIPressAdd",
                    Type = "When",
                    Regex = "^I press add$",
                    SourceLocation = @"X:\ProjectMock\CalculatorSteps.cs|12|5"
                },
                new StepDefinition
                {
                    Method = "ThenTheResultShouldBeOnTheScreen",
                    ParamTypes = "i",
                    Type = "Then",
                    Regex = "^the result should be (.*) on the screen$",
                    SourceLocation = @"X:\ProjectMock\CalculatorSteps.cs|18|5"
                },
            };
        }

        private class ConfigSettingData
        {
            public string Setting { get; set; }
            public string Value { get; set; }
        }

        [Given(@"the project configuration contains")]
        public void GivenTheProjectConfigurationContains(Table configSettingsTable)
        {
            var settings = configSettingsTable.CreateSet<ConfigSettingData>();
            foreach (var configSetting in settings)
            {
                switch (configSetting.Setting)
                {
                    case "DefaultFeatureLanguage":
                        _projectScope.DeveroomConfiguration.DefaultFeatureLanguage = configSetting.Value;
                        break;
                }
            }
            _projectScope.DeveroomConfiguration.CheckConfiguration();
            _projectScope.DeveroomConfiguration.ConfigurationChangeTime = DateTime.Now;
        }

        [Given(@"the project configuration file contains")]
        public void GivenTheProjectConfigurationFileContains(string jsonSnippet)
        {
            string configFileContent = "{" + jsonSnippet + "}";
            var configLoader = new DeveroomConfigurationLoader();
            configLoader.Update(_projectScope.DeveroomConfiguration, configFileContent, _projectScope.ProjectFolder);
            _projectScope.DeveroomConfiguration.CheckConfiguration();
            _projectScope.DeveroomConfiguration.ConfigurationChangeTime = DateTime.Now;
        }

        [Given(@"the project is configured for SpecSync with Azure DevOps project URL ""([^""]*)""")]
        public void GivenTheProjectIsConfiguredForSpecSyncWithAzureDevOpsProjectURL(string projectUrl)
        {
            string specSyncConfigFileContent = @"{
                    'remote': {
                        'projectUrl': '" + projectUrl + @"',
                    }
                }";
            ProjectScopeDeveroomConfigurationProvider.UpdateFromSpecSyncJsonConfig(_projectScope.DeveroomConfiguration, specSyncConfigFileContent);
            _projectScope.DeveroomConfiguration.CheckConfiguration();
            _projectScope.DeveroomConfiguration.ConfigurationChangeTime = DateTime.Now;
        }

        [When(@"a new step definition is added to the project as:")]
        [Given(@"the following step definitions in the project:")]
        public void WhenANewStepDefinitionIsAddedToTheProjectAs(Table stepDefinitionTable)
        {
            var stepDefinitions = stepDefinitionTable.CreateSet(CreateStepDefinitionFromTableRow).ToArray();
            RegisterStepDefinitions(stepDefinitions);
        }

        private StepDefinition CreateStepDefinitionFromTableRow(TableRow tableRow)
        {
            var stepDefinition = new StepDefinition
            {
                Method = $"M{Guid.NewGuid():N}",
                SourceLocation = @"X:\ProjectMock\CalculatorSteps.cs|12|5"
            };

            tableRow.TryGetValue("tag scope", out var tagScope);
            tableRow.TryGetValue("feature scope", out var featureScope);
            tableRow.TryGetValue("scenario scope", out var scenarioScope);

            if (string.IsNullOrEmpty(tagScope))
                tagScope = null;
            if (string.IsNullOrEmpty(featureScope))
                featureScope = null;
            if (string.IsNullOrEmpty(scenarioScope))
                scenarioScope = null;

            if (tagScope != null || featureScope != null || scenarioScope != null)
            {
                stepDefinition.Scope = new StepScope
                {
                    Tag = tagScope,
                    FeatureTitle = featureScope,
                    ScenarioTitle = scenarioScope
                };
            }

            return stepDefinition;
        }

        private void RegisterStepDefinitions(params StepDefinition[] stepDefinitions)
        {
            _discoveryService.LastDiscoveryResult = new DiscoveryResult
            {
                StepDefinitions = _discoveryService.LastDiscoveryResult.StepDefinitions.Concat(
                    stepDefinitions
                ).ToArray()
            };
        }

        [Given(@"the following C\# step definition class in the editor")]
        public void GivenTheFollowingCStepDefinitionClassInTheEditor(string stepDefinitionClass)
        {
            var fileName = "Steps.cs";
            var stepDefinitionFile =
                string.Join(Environment.NewLine, new[]
                {
                    "using System;",
                    "using TechTalk.SpecFlow;",
                    "",
                    "namespace MyProject",
                    "{",
                    stepDefinitionClass,
                    "}"
                });
            var namespaceValue = Regex.Match(stepDefinitionFile, @"namespace (?<value>\S+)").Groups["value"].Value;
            var classValue = Regex.Match(stepDefinitionFile, @"public class (?<value>\S+)").Groups["value"].Value;
            var methodValue = Regex.Match(stepDefinitionFile, @"public void (?<value>[^\(\s]+)").Groups["value"].Value;
            var stepDefTypeValue = Regex.Match(stepDefinitionFile, @"\[(?<value>Given|When|Then)\(").Groups["value"].Value;
            var regexValue = Regex.Match(stepDefinitionFile, @"\[(?:Given|When|Then)\(\@?""(?<value>.*?)""\)\]").Groups["value"].Value;
            var locationMatch = Regex.Match(stepDefinitionFile, @"^(?<line>.*\r\n)*\s*\[(?:Given|When|Then)");

            var stepDefinition = new StepDefinition()
            {
                Regex = regexValue,
                Method = $"{namespaceValue}.{classValue}.{methodValue}",
                ParamTypes = "", 
                Type = stepDefTypeValue,
                SourceLocation = $"{fileName}|{locationMatch.Groups["line"].Captures.Count + 3}|1"
            };
            _ideScope.Logger.LogInfo(stepDefinition.SourceLocation);
            RegisterStepDefinitions(stepDefinition);
            WhenTheProjectIsBuilt();
            _wpfTextView = StubWpfTextView.CreateTextView(_ideScope, new TestText(stepDefinitionFile), projectScope: _projectScope, contentType: "text", filePath: fileName);
        }


        [When(@"the project is built")]
        public void WhenTheProjectIsBuilt()
        {
            _discoveryService.LastVersion = DateTime.UtcNow;
            _discoveryService.IsDiscoveryPerformed = false;
            _ideScope.TriggerProjectsBuilt();
        }

        [Given(@"the following feature file ""([^""]*)""")]
        public void GivenTheFollowingFeatureFile(string fileName, string fileContent)
        {
            var filePath = Path.Combine(_projectScope.ProjectFolder, fileName);
            File.WriteAllText(filePath, fileContent);
            _projectScope.FilesAdded.Add(filePath, fileContent);
            //TODO: use in-memory file system?
        }


        [Given(@"the following feature file in the editor")]
        [When(@"the following feature file is opened in the editor")]
        public void GivenTheFollowingFeatureFileInTheEditor(string featureFileContent)
        {
            var fileName = "Feature1.feature";
            var filePath = Path.Combine(_projectScope.ProjectFolder, fileName);
            _projectScope.FilesAdded.Add(filePath, featureFileContent);

            _wpfTextView = StubWpfTextView.CreateTextView(_ideScope, new TestText(featureFileContent), projectScope: _projectScope);
        }
        
        [Given(@"the initial binding discovery is performed")]
        [When(@"the initial binding discovery is performed")]
        [When(@"the binding discovery is performed")]
        public void WhenTheBindingDiscoveryIsPerformed()
        {
            Wait.For(() => _discoveryService.IsDiscoveryPerformed.Should().BeTrue());
        }

        [When(@"I invoke the ""(.*)"" command by typing ""(.*)""")]
        public void WhenIInvokeTheCommandByTyping(string commandName, string typedText)
        {
            PerformCommand(commandName, typedText);
        }

        [Given(@"the ""(.*)"" command has been invoked")]
        [When(@"I invoke the ""(.*)"" command")]
        public void WhenIInvokeTheCommand(string commandName)
        {
            PerformCommand(commandName);
        }

        private void PerformCommand(string commandName, string parameter = null, DeveroomEditorCommandTargetKey? commandTargetKey = null)
        {
            ActionsMock.ResetMock();
            switch (commandName)
            {
                case "Go To Definition":
                {
                    var command = new GoToStepDefinitionCommand(_ideScope,
                        new StubBufferTagAggregatorFactoryService(_ideScope), _ideScope.MonitoringService);
                    command.PreExec(_wpfTextView, command.Targets.First());
                    break;
                }
                case "Find Step Definition Usages":
                {
                    var command = new FindStepDefinitionCommand(_ideScope,
                        new StubBufferTagAggregatorFactoryService(_ideScope), _ideScope.MonitoringService);
                    command.PreExec(_wpfTextView, command.Targets.First());
                    Wait.For(() => ActionsMock.IsComplete.Should().BeTrue());
                    break;
                }
                case "Comment":
                {
                    var command = new CommentCommand(_ideScope,
                        new StubBufferTagAggregatorFactoryService(_ideScope), _ideScope.MonitoringService);
                    command.PreExec(_wpfTextView, command.Targets.First());
                    break;
                }
                case "Uncomment":
                {
                    var command = new UncommentCommand(_ideScope,
                        new StubBufferTagAggregatorFactoryService(_ideScope), _ideScope.MonitoringService);
                    command.PreExec(_wpfTextView, command.Targets.First());
                    break;
                }
                case "Auto Format Table":
                {
                    var command = new AutoFormatTableCommand(_ideScope,
                        new StubBufferTagAggregatorFactoryService(_ideScope), _ideScope.MonitoringService);
                    _wpfTextView.SimulateType(command, parameter?[0] ?? '|');
                    break;
                }
                case "Define Steps":
                {
                    var command = new DefineStepsCommand(_ideScope,
                        new StubBufferTagAggregatorFactoryService(_ideScope), _ideScope.MonitoringService);
                    command.PreExec(_wpfTextView, command.Targets.First());
                    break;
                }
                case "Complete":
                case "Filter Completion":
                {
                    EnsureStubCompletionBroker();
                    var command = new CompleteCommand(_ideScope,
                        new StubBufferTagAggregatorFactoryService(_ideScope),
                        _completionBroker, _ideScope.MonitoringService);
                    if (parameter == null)
                        command.PreExec(_wpfTextView, commandTargetKey ?? command.Targets.First());
                    else
                        _wpfTextView.SimulateTypeText(command, parameter);
                    break;
                }
                default:
                    throw new NotImplementedException(commandName);
            }
        }

        private void EnsureStubCompletionBroker()
        {
            if (_completionBroker != null)
                return;

            var textBuffer = _wpfTextView.TextBuffer;
            var completionSource = new DeveroomCompletionSource(
                textBuffer,
                new StubBufferTagAggregatorFactoryService(_ideScope).CreateTagAggregator<DeveroomTag>(textBuffer),
                _ideScope);
            _completionBroker = new StubCompletionBroker(completionSource);
        }

        [When(@"commit the ""([^""]*)"" completion item")]
        public void WhenCommitTheCompletionItem(string value)
        {
            EnsureStubCompletionBroker();
            //TODO: select item
            var session = _completionBroker.GetSessions(_wpfTextView).FirstOrDefault();
            session.Should().NotBeNull("There should be an active completion session");
            var completionSet = session.SelectedCompletionSet;
            completionSet.Should().NotBeNull("There should be an active completion set");
            var completion = completionSet.Completions.FirstOrDefault(c => c.InsertionText.StartsWith(value));
            completion.Should().NotBeNull($"There should be a completion item starting with '{value}'");
            completionSet.SelectionStatus = 
                new CompletionSelectionStatus(completion, true, true);
            PerformCommand("Complete", null, CompletionCommandBase.ReturnCommand);
        }


        [Then(@"the editor should be updated to")]
        public void ThenTheEditorShouldBeUpdatedTo(string expectedContentValue)
        {
            var expectedContent = new TestText(expectedContentValue);
            Assert.Equal(expectedContent.ToString(), _wpfTextView.TextSnapshot.GetText());
        }

        private IEnumerable<DeveroomTag> GetDeveroomTags(IWpfTextView textView)
        {
            var tagger = DeveroomTaggerProvider.GetDeveroomTagger(textView.TextBuffer);
            if (tagger != null)
            {
                return GetVsTagSpans<DeveroomTag, DeveroomTagger>(textView, tagger).Select(t => t.Tag);
            }
            return Enumerable.Empty<DeveroomTag>();
        }

        private IEnumerable<ITagSpan<TTag>> GetVsTagSpans<TTag>(IWpfTextView textView, ITaggerProvider taggerProvider) where TTag: ITag
        {
            var tagger = taggerProvider.CreateTagger<TTag>(textView.TextBuffer);
            return GetVsTagSpans<TTag, ITagger<TTag>>(textView, tagger);
        }

        private IEnumerable<ITagSpan<TTag>> GetVsTagSpans<TTag, TTagger>(IWpfTextView textView, TTagger tagger) where TTag : ITag where TTagger : ITagger<TTag>
        {
            var spans = new NormalizedSnapshotSpanCollection(new SnapshotSpan(textView.TextSnapshot, 0, textView.TextSnapshot.Length));
            return tagger.GetTags(spans);
        }

        [Then(@"all section of types (.*) should be highlighted as")]
        public void ThenAllSectionOfTypesShouldBeHighlightedAs(string[] keywordTypes, string expectedContent)
        {
            var expectedContentText = new TestText(expectedContent);
            var tags = GetDeveroomTags(_wpfTextView).Where(t => keywordTypes.Contains(t.Type)).ToArray();
            var testTextSections = expectedContentText.Sections.Where(s => keywordTypes.Contains(s.Label)).ToArray();
            testTextSections.Should().NotBeEmpty("there should be something to expect");
            foreach (var section in testTextSections)
            {
                tags.Should().Contain(t =>
                        t.Type == section.Label &&
                        t.Span.Start == expectedContentText.GetSnapshotPoint(t.Span.Snapshot, section.Start.Line, section.Start.Column) &&
                        t.Span.End == expectedContentText.GetSnapshotPoint(t.Span.Snapshot, section.End.Line, section.End.Column),
                    $"the section '{section}' should be highlighted"
                );
            }
            tags.Should().HaveCount(testTextSections.Length);
        }

        [Then(@"no binding error should be highlighted")]
        public void ThenNoBindingErrorShouldBeHighlighted()
        {
            var tags = GetDeveroomTags(_wpfTextView).ToArray();
            tags.Should().NotContain(t => t.Type == "BindingError");
        }
        
        [Then(@"all (.*) section should be highlighted as")]
        public void ThenTheStepKeywordsShouldBeHighlightedAs(string keywordType, string expectedContent)
        {
            ThenAllSectionOfTypesShouldBeHighlightedAs(new[] {keywordType}, expectedContent);
        }

        [Then(@"the tag links should target to the following URLs")]
        public void ThenTheTagLinksShouldTargetToTheFollowingURLs(Table expectedTagLinksTable)
        {
            var tagSpans = GetVsTagSpans<UrlTag>(_wpfTextView, new DeveroomUrlTaggerProvider(new StubBufferTagAggregatorFactoryService(_ideScope), _ideScope)).ToArray();
            var actualTagLinks = tagSpans.Select(t => new {Tag = t.Span.GetText(), URL = t.Tag.Url.ToString()});
            expectedTagLinksTable.CompareToSet(actualTagLinks);
        }

        [Then(@"the source file of the ""(.*)"" ""(.*)"" step definition is opened")]
        public void ThenTheSourceFileOfTheStepDefinitionIsOpened(string stepRegex, ScenarioBlock stepType)
        {
            _stepDefinitionBinding = _discoveryService.GetBindingRegistry().StepDefinitions.FirstOrDefault(b => b.StepDefinitionType == stepType && b.Regex.ToString().Contains(stepRegex));
            _stepDefinitionBinding.Should().NotBeNull($"there has to be a {stepType} stepdef with regex '{stepRegex}'");

            ActionsMock.LastNavigateToSourceLocation.Should().NotBeNull();
            ActionsMock.LastNavigateToSourceLocation.SourceFile.Should().Be(_stepDefinitionBinding.Implementation.SourceLocation.SourceFile);
        }

        [Then(@"the caret is positioned to the step definition method")]
        public void ThenTheCaretIsPositionedToTheStepDefinitionMethod()
        {
            ActionsMock.LastNavigateToSourceLocation.Should().Be(_stepDefinitionBinding.Implementation.SourceLocation);
        }

        [Then(@"a jump list ""(.*)"" is opened with the following items")]
        public void ThenAJumpListIsOpenedWithTheFollowingItems(string expectedHeader, Table expectedJumpListItemsTable)
        {
            var expectedStepDefinitions = expectedJumpListItemsTable.Rows.Select(r => r[0]).ToArray();
            ActionsMock.LastShowContextMenuHeader.Should().Be(expectedHeader);
            ActionsMock.LastShowContextMenuItems.Should().NotBeNull();
            var actualStepDefs = ActionsMock.LastShowContextMenuItems.Select(i => Regex.Match(i.Label, @"\((?<stepdef>.*?)\)").Groups["stepdef"].Value).ToArray();
            actualStepDefs.Should().Equal(expectedStepDefinitions);
        }

        [Then(@"a jump list ""(.*)"" is opened with the following steps")]
        public void ThenAJumpListIsOpenedWithTheFollowingSteps(string expectedHeader, Table expectedJumpListItemsTable)
        {
            var expectedStepDefinitions = expectedJumpListItemsTable.Rows.Select(r => r[0]).ToArray();
            ActionsMock.LastShowContextMenuHeader.Should().Be(expectedHeader);
            ActionsMock.LastShowContextMenuItems.Should().NotBeNull();
            var actualStepDefs = ActionsMock.LastShowContextMenuItems.Select(i => i.Label).ToArray();
            actualStepDefs.Should().Equal(expectedStepDefinitions);
        }

        private void InvokeFirstContextMenuItem()
        {
            var firstItem = ActionsMock.LastShowContextMenuItems.ElementAtOrDefault(0);
            firstItem.Should().NotBeNull();

            // invoke the command
            firstItem.Command(firstItem);
        }

        [Then(@"invoking the first item from the jump list navigates to the ""(.*)"" ""(.*)"" step definition")]
        public void ThenInvokingTheFirstItemFromTheJumpListNavigatesToTheStepDefinition(string stepRegex, ScenarioBlock stepType)
        {
            InvokeFirstContextMenuItem();

            ThenTheSourceFileOfTheStepDefinitionIsOpened(stepRegex, stepType);
        }

        [Then(@"invoking the first item from the jump list navigates to the ""([^""]*)"" step in ""([^""]*)"" line (.*)")]
        public void ThenInvokingTheFirstItemFromTheJumpListNavigatesToTheStepInLine(string step, string expectedFile, int expectedLine)
        {
            InvokeFirstContextMenuItem();

            ActionsMock.LastNavigateToSourceLocation.Should().NotBeNull();
            ActionsMock.LastNavigateToSourceLocation.SourceFile.Should().EndWith(expectedFile);
            ActionsMock.LastNavigateToSourceLocation.SourceFileLine.Should().Be(expectedLine);
        }

        [Then(@"the step definition skeleton for the ""(.*)"" ""(.*)"" step should be offered to copy to clipboard")]
        public void ThenTheStepDefinitionSkeletonForTheStepShouldBeOfferedToCopyToClipboard(string stepText, ScenarioBlock stepType)
        {
            ActionsMock.LastShowQuestion.Should().NotBeNull();
            ActionsMock.LastShowQuestion.Description.Should().Contain(stepText);
            ActionsMock.LastShowQuestion.Description.Should().Contain(stepType.ToString());

            ActionsMock.LastShowQuestion.YesCommand.Should().NotBeNull();
        }


        [Then(@"there should be no navigation actions performed")]
        public void ThenThereShouldBeNoNavigationActionsPerformed()
        {
            // neither navigation nor jump list
            ActionsMock.LastNavigateToSourceLocation.Should().BeNull();
            ActionsMock.LastShowContextMenuItems.Should().BeNull();
        }

        class StepDefinitionSnippetData
        {
            public string Type { get; set; }
            public string Regex { get; set; }
        }

        private StepDefinitionSnippetData[] ParseSnippets(string text)
        {
            var regex = new Regex(@"\[(?<type>Given|When|Then)\(\@""(?<regex>[^\n]+)""\)\]");
            var matches = regex.Matches(text);
            return matches.Cast<Match>().Select(m =>
                new StepDefinitionSnippetData()
                {
                    Type = m.Groups["type"].Value,
                    Regex = m.Groups["regex"].Value
                }).ToArray();
        }

        [Then(@"the define steps dialog should be opened with the following step definition skeletons")]
        public void ThenTheDefineStepsDialogShouldBeOpenedWithTheFollowingStepDefinitionSkeletons(Table expectedSkeletons)
        {
            var viewModel = _ideScope.StubWindowManager.GetShowDialogViewModel<CreateStepDefinitionsDialogViewModel>();
            viewModel.Should().NotBeNull("the 'define steps' dialog should have been opened");

            var parsedSnippets = viewModel.Items.Select(i => ParseSnippets(i.Snippet).First()).ToArray();
            expectedSkeletons.CompareToSet(parsedSnippets, false);
        }

        [Then(@"a (.*) dialog should be opened with ""(.*)""")]
        public void ThenAShowProblemDialogShouldBeOpenedWith(string expectedDialog, string expectedMessage)
        {
            _ideScope.StubLogger.Messages.Should()
                .Contain(m => m.Item2.Contains(expectedDialog) && m.Item2.Contains(expectedMessage));
        }

        [Given(@"the ""(.*)"" command is being invoked")]
        public void GivenTheCommandIsBeingInvoked(string command)
        {
            _commandToInvokeDeferred = command;
        }

        [When(@"I select the step definition snippets (.*)")]
        public void WhenISelectTheStepDefinitionSnippets(int[] indicesToSelect)
        {
            _ideScope.StubWindowManager.RegisterWindowAction<CreateStepDefinitionsDialogViewModel>(
                viewModel =>
                {
                    foreach (var item in viewModel.Items)
                        item.IsSelected = false;
                    foreach (var i in indicesToSelect)
                        viewModel.Items[i].IsSelected = true;
                });
        }

        [When(@"close the define steps dialog with ""(.*)""")]
        public void WhenCloseTheDefineStepsDialogWith(string button)
        {
            _ideScope.StubWindowManager.RegisterWindowAction<CreateStepDefinitionsDialogViewModel>(
                viewModel =>
                {
                    switch (button.ToLowerInvariant())
                    {
                        case "copy to clipboard":
                            viewModel.Result = CreateStepDefinitionsDialogResult.CopyToClipboard;
                            break;
                        case "create":
                            viewModel.Result = CreateStepDefinitionsDialogResult.Create;
                            break;
                    }
                });
            WhenIInvokeTheCommand(_commandToInvokeDeferred);
        }

        [Then(@"the following step definition snippets should be copied to the clipboard")]
        public void ThenTheFollowingStepDefinitionSnippetsShouldBeCopiedToTheClipboard(Table expectedSnippets)
        {
            ActionsMock.ClipboardText.Should().NotBeNull("snippets should have been copied to clipboard");
            var parsedSnippets = ParseSnippets(ActionsMock.ClipboardText);
            expectedSnippets.CompareToSet(parsedSnippets, false);
        }

        [Then(@"the following step definition snippets should be in file ""(.*)""")]
        public void ThenTheFollowingStepDefinitionSnippetsShouldBeInFile(string fileName, Table expectedSnippets)
        {
            var filePath = Path.Combine(_projectScope.ProjectFolder, fileName);
            var fileAdded = _projectScope.FilesAdded.TryGetValue(filePath, out var fileContent);
            fileAdded.Should().BeTrue($"file '{filePath}' should have been created");
            //File.Exists(filePath).Should().BeTrue($"file '{filePath}' should have been created");
            //var fileContent = File.ReadAllText(filePath);
            var parsedSnippets = ParseSnippets(fileContent);
            expectedSnippets.CompareToSet(parsedSnippets, false);
        }

        [Then(@"a completion list should pop up with the following items")]
        [Then(@"a completion list should list the following items")]
        public void ThenACompletionListShouldPopUpWithTheFollowingItems(Table expectedItemsTable)
        {
            CheckCompletions(expectedItemsTable);
        }

        [Then(@"a completion list should pop up with the following keyword items")]
        public void ThenACompletionListShouldPopUpWithTheFollowingKeywordItems(Table expectedItemsTable)
        {
            CheckCompletions(expectedItemsTable, t => char.IsLetter(t[0]));
        }

        [Then(@"a completion list should pop up with the following markers")]
        public void ThenACompletionListShouldPopUpWithTheFollowingMarkers(Table expectedItemsTable)
        {
            CheckCompletions(expectedItemsTable, t => t.All(c => !char.IsLetter(c)));
        }

        private void CheckCompletions(Table expectedItemsTable, Func<string, bool> filter = null)
        {
            _completionBroker.Should().NotBeNull();
            var actualCompletions = _completionBroker.Completions
                .Where(c => filter?.Invoke(c.InsertionText) ?? true)
                .Select(c => new {Item = c.InsertionText.Trim(), c.Description});

            expectedItemsTable.CompareToSet(actualCompletions, false);
        }
    }
}
