using ScenarioBlock = SpecFlow.VisualStudio.Editor.Services.Parser.ScenarioBlock;

namespace SpecFlow.VisualStudio.Specs.StepDefinitions;

[Binding]
public class ProjectSystemSteps : Steps
{
    private readonly StubIdeScope _ideScope;
    private string _commandToInvokeDeferred;
    private StubCompletionBroker _completionBroker;
    private MockableDiscoveryService _discoveryService;
    private InMemoryStubProjectScope _projectScope;
    private ProjectStepDefinitionBinding _stepDefinitionBinding;
    private StubWpfTextView _wpfTextView;

    public ProjectSystemSteps(StubIdeScope stubIdeScope)
    {
        _ideScope = stubIdeScope;
    }

    private StubIdeActions ActionsMock => (StubIdeActions) _ideScope.Actions;

    [Given(@"there is a SpecFlow project scope")]
    public void GivenThereIsASpecFlowProjectScope()
    {
        _projectScope = new InMemoryStubProjectScope(_ideScope);
        _projectScope.AddSpecFlowPackage();
        _discoveryService = MockableDiscoveryService.Setup(_projectScope, TimeSpan.FromMilliseconds(100));
    }

    [Given(@"there is a SpecFlow project scope with calculator step definitions")]
    public void GivenThereIsASpecFlowProjectScopeWithCalculatorStepDefinitions()
    {
        GivenThereIsASpecFlowProjectScope();
        var filePath = @"X:\ProjectMock\CalculatorSteps.cs";
        _discoveryService.LastDiscoveryResult.StepDefinitions = new[]
        {
            new StepDefinition
            {
                Method = "GivenIHaveEnteredIntoTheCalculator",
                ParamTypes = "i",
                Type = "Given",
                Regex = "^I have entered (.*) into the calculator$",
                SourceLocation = filePath + "|24|5"
            },
            new StepDefinition
            {
                Method = "WhenIPressAdd",
                Type = "When",
                Regex = "^I press add$",
                SourceLocation = filePath + "|12|5"
            },
            new StepDefinition
            {
                Method = "ThenTheResultShouldBeOnTheScreen",
                ParamTypes = "i",
                Type = "Then",
                Regex = "^the result should be (.*) on the screen$",
                SourceLocation = filePath + "|18|5"
            }
        };

        _projectScope.AddFile(filePath, string.Empty);
    }

    [Given(@"the specflow.json configuration file contains")]
    public void GivenTheSpecFlowJsonConfigurationFileContains(string configFileContent)
    {
        ProjectScopeDeveroomConfigurationProvider.UpdateFromSpecFlowJsonConfig(_projectScope.DeveroomConfiguration,
            configFileContent, _projectScope.ProjectFolder);
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
        ProjectScopeDeveroomConfigurationProvider.UpdateFromSpecSyncJsonConfig(_projectScope.DeveroomConfiguration,
            specSyncConfigFileContent);
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
        var filePath = @"X:\ProjectMock\CalculatorSteps.cs";
        var stepDefinition = new StepDefinition
        {
            Method = $"M{Guid.NewGuid():N}",
            SourceLocation = filePath + "|12|5"
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
            stepDefinition.Scope = new StepScope
            {
                Tag = tagScope,
                FeatureTitle = featureScope,
                ScenarioTitle = scenarioScope
            };

        _projectScope.AddFile(filePath, string.Empty);

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

    [Given(@"^the following C\# step definition class$")]
    [Given(@"^the following C\# step definition class in the editor$")]
    public void GivenTheFollowingCStepDefinitionClassInTheEditor(string stepDefinitionClass)
    {
        var fileName = DomainDefaults.StepDefinitionFileName;
        var filePath = Path.Combine(_projectScope.ProjectFolder, fileName);
        var stepDefinitionFile = GetStepDefinitionFileContentFromClass(stepDefinitionClass);

        var stepDefinitions = ParseStepDefinitions(stepDefinitionFile, filePath);

        RegisterStepDefinitions(stepDefinitions.ToArray());
        _wpfTextView = _ideScope.CreateTextView(new TestText(stepDefinitionFile), projectScope: _projectScope,
            contentType: VsContentTypes.CSharp, filePath: fileName);
    }

    private static string GetStepDefinitionFileContentFromClass(string stepDefinitionClass)
    {
        return string.Join(Environment.NewLine, "using System;", "using TechTalk.SpecFlow;", "", "namespace MyProject",
            "{", stepDefinitionClass, "}");
    }

    private static string GetStepDefinitionClassFromMethod(string stepDefinitionMethod)
    {
        return string.Join(Environment.NewLine, "[Binding]", "public class StepDefinitions1", "{", stepDefinitionMethod,
            "}");
    }

    private List<StepDefinition> ParseStepDefinitions(string stepDefinitionFileContent, string filePath)
    {
        var stepDefinitions = new List<StepDefinition>();

        var tree = CSharpSyntaxTree.ParseText(stepDefinitionFileContent);
        var rootNode = tree.GetRoot();
        var nsDeclaration = rootNode.DescendantNodes().OfType<NamespaceDeclarationSyntax>().First();
        var methods = rootNode.DescendantNodes().OfType<MethodDeclarationSyntax>().ToArray();
        foreach (var method in methods)
        {
            var classDeclarationSyntax = method.Ancestors().OfType<ClassDeclarationSyntax>().First();
            Debug.Assert(method.Body != null);
            var methodLineNumber = method.SyntaxTree.GetLineSpan(method.Body.Span).StartLinePosition.Line + 1;

            var stepDefinitionAttributes =
                RenameStepStepDefinitionClassAction.GetAttributesWithTokens(method)
                    .Where(awt => !awt.Item2.IsMissing)
                    .ToArray();

            foreach (var (attributeSyntax, stepDefinitionAttributeTextToken) in stepDefinitionAttributes)
            {
                var stepDefinition = new StepDefinition
                {
                    Regex = "^" + stepDefinitionAttributeTextToken.ValueText + "$",
                    Method = $"{nsDeclaration.Name}.{classDeclarationSyntax.Identifier.Text}.{method.Identifier.Text}",
                    ParamTypes = "",
                    Type = attributeSyntax?.Name.ToString(),
                    SourceLocation = $"{filePath}|{methodLineNumber}|1",
                    Expression = stepDefinitionAttributeTextToken.ValueText
                };

                _ideScope.Logger.LogInfo(
                    $"{stepDefinition.SourceLocation}: {stepDefinition.Type}/{stepDefinition.Regex}");
                stepDefinitions.Add(stepDefinition);
            }
        }

        return stepDefinitions;
    }

    [When(@"the project is built")]
    public void WhenTheProjectIsBuilt()
    {
        _discoveryService.Invalidate();
        _ideScope.TriggerProjectsBuilt();
    }

    [When("the project is built and the initial binding discovery is performed")]
    [Given("the project is built and the initial binding discovery is performed")]
    public async Task GivenTheProjectIsBuiltAndTheInitialBindingDiscoveryIsPerformed()
    {
        WhenTheProjectIsBuilt();
        await WhenTheBindingDiscoveryIsPerformed();
    }


    [Given(@"the following feature file ""([^""]*)""")]
    public void GivenTheFollowingFeatureFile(string fileName, string fileContent)
    {
        var filePath = Path.Combine(_projectScope.ProjectFolder, fileName);
        _ideScope.FileSystem.Directory.CreateDirectory(_projectScope.ProjectFolder);
        _ideScope.FileSystem.File.WriteAllText(filePath, fileContent);
        _projectScope.FilesAdded.Add(filePath, fileContent);
    }


    [Given(@"the following feature file in the editor")]
    [When(@"the following feature file is opened in the editor")]
    public void GivenTheFollowingFeatureFileInTheEditor(string featureFileContent)
    {
        var fileName = "Feature1.feature";
        var filePath = Path.Combine(_projectScope.ProjectFolder, fileName);

        _wpfTextView = _ideScope.CreateTextView(new TestText(featureFileContent), projectScope: _projectScope,
            filePath: filePath);
        GivenTheFollowingFeatureFile(fileName, _wpfTextView.TextBuffer.CurrentSnapshot.GetText());
        //WhenTheProjectIsBuilt();
    }

    [Given(@"the initial binding discovery is performed")]
    [When(@"the initial binding discovery is performed")]
    [When(@"the binding discovery is performed")]
    public async Task WhenTheBindingDiscoveryIsPerformed()
    {
        await _discoveryService.WaitUntilDiscoveryPerformed();
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

    private void PerformCommand(string commandName, string parameter = null,
        DeveroomEditorCommandTargetKey? commandTargetKey = null)
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
                var command = new FindStepDefinitionUsagesCommand(_ideScope,
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
            case "Auto Format Document":
            {
                var command = new AutoFormatDocumentCommand(_ideScope,
                    new StubBufferTagAggregatorFactoryService(_ideScope), _ideScope.MonitoringService,
                    new GherkinDocumentFormatter());
                command.PreExec(_wpfTextView, AutoFormatDocumentCommand.FormatDocumentKey);
                break;
            }
            case "Auto Format Selection":
            {
                var command = new AutoFormatDocumentCommand(_ideScope,
                    new StubBufferTagAggregatorFactoryService(_ideScope), _ideScope.MonitoringService,
                    new GherkinDocumentFormatter());
                command.PreExec(_wpfTextView, AutoFormatDocumentCommand.FormatSelectionKey);
                break;
            }
            case "Auto Format Table":
            {
                var command = new AutoFormatTableCommand(_ideScope,
                    new StubBufferTagAggregatorFactoryService(_ideScope), _ideScope.MonitoringService,
                    new GherkinDocumentFormatter());
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
            case "Rename Step":
            {
                var command = new RenameStepCommand(_ideScope,
                    new StubBufferTagAggregatorFactoryService(_ideScope), _ideScope.MonitoringService);
                command.PreExec(_wpfTextView, command.Targets.First());

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

    [Then("the editor should be updated to contain")]
    public void ThenTheEditorShouldBeUpdatedToContain(string expectedContentValue)
    {
        var expectedContent = new TestText(expectedContentValue).ToString();
        var currentContent = _ideScope.CurrentTextView.TextSnapshot.GetText();
        currentContent.Should().Contain(expectedContent);
    }

    private IEnumerable<DeveroomTag> GetDeveroomTags(IWpfTextView textView)
    {
        var tagger = DeveroomTaggerProvider.GetDeveroomTagger(textView.TextBuffer);
        if (tagger != null) return GetVsTagSpans<DeveroomTag, DeveroomTagger>(textView, tagger).Select(t => t.Tag);
        return Enumerable.Empty<DeveroomTag>();
    }

    private IEnumerable<ITagSpan<TTag>> GetVsTagSpans<TTag>(IWpfTextView textView, ITaggerProvider taggerProvider)
        where TTag : ITag
    {
        var tagger = taggerProvider.CreateTagger<TTag>(textView.TextBuffer);
        return GetVsTagSpans<TTag, ITagger<TTag>>(textView, tagger);
    }

    private IEnumerable<ITagSpan<TTag>> GetVsTagSpans<TTag, TTagger>(IWpfTextView textView, TTagger tagger)
        where TTag : ITag where TTagger : ITagger<TTag>
    {
        var spans = new NormalizedSnapshotSpanCollection(new SnapshotSpan(textView.TextSnapshot, 0,
            textView.TextSnapshot.Length));
        return tagger.GetTags(spans);
    }

    [Then(@"all section of types (.*) should be highlighted as")]
    public void ThenAllSectionOfTypesShouldBeHighlightedAs(string[] keywordTypes, string expectedContent)
    {
        var expectedContentText = new TestText(expectedContent);
        var tags = GetDeveroomTags(_wpfTextView).Where(t => keywordTypes.Contains(t.Type)).ToArray();
        var testTextSections = expectedContentText.Sections.Where(s => keywordTypes.Contains(s.Label)).ToArray();
        testTextSections.Should().NotBeEmpty("there should be something to expect");
        var matchedTags = tags.ToList();
        foreach (var section in testTextSections)
        {
            var matchedTag = tags.FirstOrDefault(
                t =>
                    t.Type == section.Label &&
                    t.Span.Start == expectedContentText.GetSnapshotPoint(t.Span.Snapshot, section.Start.Line,
                        section.Start.Column) &&
                    t.Span.End ==
                    expectedContentText.GetSnapshotPoint(t.Span.Snapshot, section.End.Line, section.End.Column)
            );
            matchedTag.Should().NotBeNull($"the section '{section}' should be highlighted");
            matchedTags.Remove(matchedTag);
        }

        matchedTags.Should().BeEmpty();
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
        var tagSpans = GetVsTagSpans<UrlTag>(_wpfTextView,
            new DeveroomUrlTaggerProvider(new StubBufferTagAggregatorFactoryService(_ideScope), _ideScope)).ToArray();
        var actualTagLinks = tagSpans.Select(t => new {Tag = t.Span.GetText(), URL = t.Tag.Url.ToString()});
        expectedTagLinksTable.CompareToSet(actualTagLinks);
    }

    [Then(@"the source file of the ""(.*)"" ""(.*)"" step definition is opened")]
    public void ThenTheSourceFileOfTheStepDefinitionIsOpened(string stepRegex, ScenarioBlock stepType)
    {
        _stepDefinitionBinding = _discoveryService.GetBindingRegistry().StepDefinitions
            .FirstOrDefault(b => b.StepDefinitionType == stepType && b.Regex.ToString().Contains(stepRegex));
        _stepDefinitionBinding.Should().NotBeNull($"there has to be a {stepType} stepdef with regex '{stepRegex}'");

        ActionsMock.LastNavigateToSourceLocation.Should().NotBeNull();
        ActionsMock.LastNavigateToSourceLocation.SourceFile.Should()
            .Be(_stepDefinitionBinding.Implementation.SourceLocation.SourceFile);
    }

    [Then(@"the caret is positioned to the step definition method")]
    public void ThenTheCaretIsPositionedToTheStepDefinitionMethod()
    {
        ActionsMock.LastNavigateToSourceLocation.Should().Be(_stepDefinitionBinding.Implementation.SourceLocation);
    }

    [Then(@"a jump list ""(.*)"" is opened with the following items")]
    public void ThenAJumpListIsOpenedWithTheFollowingItems(string expectedHeader, Table expectedJumpListItemsTable)
    {
        ActionsMock.LastShowContextMenuHeader.Should().Be(expectedHeader);
        ActionsMock.LastShowContextMenuItems.Should().NotBeNull();
        var actualStepDefs = ActionsMock.LastShowContextMenuItems.Select(
            i =>
                new StepDefinitionJumpListData
                {
                    StepDefinition = Regex.Match(i.Label, @"\((?<stepdef>.*?)\)").Groups["stepdef"].Value,
                    StepType = Regex.Match(i.Label, @"\[(?<stepdeftype>.*?)\(").Groups["stepdeftype"].Value
                }).ToArray();
        expectedJumpListItemsTable.CompareToSet(actualStepDefs);
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
    public void ThenInvokingTheFirstItemFromTheJumpListNavigatesToTheStepDefinition(string stepRegex,
        ScenarioBlock stepType)
    {
        InvokeFirstContextMenuItem();

        ThenTheSourceFileOfTheStepDefinitionIsOpened(stepRegex, stepType);
    }

    [Then(@"invoking the first item from the jump list navigates to the ""([^""]*)"" step in ""([^""]*)"" line (.*)")]
    public void ThenInvokingTheFirstItemFromTheJumpListNavigatesToTheStepInLine(string step, string expectedFile,
        int expectedLine)
    {
        InvokeFirstContextMenuItem();

        ActionsMock.LastNavigateToSourceLocation.Should().NotBeNull();
        ActionsMock.LastNavigateToSourceLocation.SourceFile.Should().EndWith(expectedFile);
        ActionsMock.LastNavigateToSourceLocation.SourceFileLine.Should().Be(expectedLine);
    }

    [Then(@"the step definition skeleton for the ""(.*)"" ""(.*)"" step should be offered to copy to clipboard")]
    public void ThenTheStepDefinitionSkeletonForTheStepShouldBeOfferedToCopyToClipboard(string stepText,
        TechTalk.SpecFlow.ScenarioBlock stepType)
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

    private StepDefinitionSnippetData[] ParseSnippetsFromFile(string text,
        string filePath = DomainDefaults.StepDefinitionFileName)
    {
        var stepDefinitions = ParseStepDefinitions(text, filePath);
        return stepDefinitions.Select(sd =>
            new StepDefinitionSnippetData
            {
                Type = sd.Type,
                Regex = sd.Regex,
                Expression = sd.Expression
            }).ToArray();
    }

    private StepDefinitionSnippetData[] ParseSnippets(string snippetText)
    {
        return ParseSnippetsFromFile(
            GetStepDefinitionFileContentFromClass(GetStepDefinitionClassFromMethod(snippetText)));
    }

    [Then(@"the define steps dialog should be opened with the following step definition skeletons")]
    public void ThenTheDefineStepsDialogShouldBeOpenedWithTheFollowingStepDefinitionSkeletons(Table expectedSkeletons)
    {
        var viewModel = _ideScope.StubWindowManager.GetShowDialogViewModel<CreateStepDefinitionsDialogViewModel>();
        viewModel.Should().NotBeNull("the 'define steps' dialog should have been opened");

        var parsedSnippets = viewModel.Items.Select(i => ParseSnippets(i.Snippet).First()).ToArray();
        expectedSkeletons.CompareToSet(parsedSnippets);
    }

    [Then(@"a (.*) dialog should be opened with ""(.*)""")]
    public void ThenAShowProblemDialogShouldBeOpenedWith(string expectedDialog, string expectedMessage)
    {
        _ideScope.StubLogger.Logs.Should()
            .Contain(m => m.Message.Contains(expectedDialog) && m.Message.Contains(expectedMessage));
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

    [When("I specify {string} as renamed step")]
    public async Task WhenISpecifyAsRenamedStep(string renamedStep)
    {
        _ideScope.StubWindowManager.RegisterWindowAction<RenameStepViewModel>(
            viewModel => { viewModel.StepText = renamedStep; });
        WhenIInvokeTheCommand(_commandToInvokeDeferred);

        await _projectScope.StubIdeScope.AnalyticsTransmitter
            .WaitForEventAsync("Rename step command executed");
    }

    [Then("invoking the first item from the jump list renames the {string} {string} step definition")]
    public async Task ThenInvokingTheFirstItemFromTheJumpListRenamesTheStepDefinition(string expression,
        string stepType)
    {
        const string renamedExpression = "renamed step";
        _ideScope.StubWindowManager.RegisterWindowAction<RenameStepViewModel>(
            viewModel =>
            {
                viewModel.StepText = renamedExpression;
                viewModel.OriginalStepText.Should()
                    .Be($"[{stepType}({expression})]: MyProject.CalculatorSteps.WhenIPressAdd");
            });

        InvokeFirstContextMenuItem();

        await _projectScope.StubIdeScope.AnalyticsTransmitter
            .WaitForEventAsync("Rename step command executed");

        string fileContent = _wpfTextView.TextSnapshot.GetText();
        var parsedSnippets = ParseSnippetsFromFile(fileContent);
        parsedSnippets.Should().Contain(s => s.Type == stepType && s.Expression == renamedExpression);
    }

    [Then(@"the following step definition snippets should be copied to the clipboard")]
    public void ThenTheFollowingStepDefinitionSnippetsShouldBeCopiedToTheClipboard(Table expectedSnippets)
    {
        ActionsMock.ClipboardText.Should().NotBeNull("snippets should have been copied to clipboard");
        var parsedSnippets = ParseSnippets(ActionsMock.ClipboardText);
        expectedSnippets.CompareToSet(parsedSnippets);
    }

    [Then(@"the editor should be updated to contain the following step definitions")]
    [Then(@"the following step definition snippets should be in the step definition class")]
    public void ThenTheFollowingStepDefinitionSnippetsShouldBeInTheStepDefinitionClass(Table expectedSnippets)
    {
        ThenTheFollowingStepDefinitionSnippetsShouldBeInFile(DomainDefaults.StepDefinitionFileName, expectedSnippets);
    }

    [Then(@"the following step definition snippets should be in file ""(.*)""")]
    public void ThenTheFollowingStepDefinitionSnippetsShouldBeInFile(string fileName, Table expectedSnippets)
    {
        string fileContent = GetActualContent(fileName);
        var filePath = Path.Combine(_projectScope.ProjectFolder, fileName);
        _projectScope.AddFile(filePath, fileContent);
        var parsedSnippets = ParseSnippetsFromFile(fileContent, filePath);
        expectedSnippets.CompareToSet(parsedSnippets);
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

        expectedItemsTable.CompareToSet(actualCompletions);
    }

    [Then("the file {string} should be updated to")]
    public void ThenTheFileShouldBeUpdatedTo(string fileName, string expectedFileContent)
    {
        var actualContent = GetActualContent(fileName);
        Assert.Equal(expectedFileContent, actualContent);
    }

    private string GetActualContent(string fileName)
    {
        var filePath = Path.Combine(_projectScope.ProjectFolder, fileName);
        if (_ideScope.OpenViews.TryGetValue(filePath, out var textView))
            return textView.TextBuffer.CurrentSnapshot.GetText();

        if (_ideScope.FileSystem.File.Exists(filePath)) return _ideScope.FileSystem.File.ReadAllText(filePath);

        var fileAdded = _projectScope.FilesAdded.TryGetValue(filePath, out var fileContent);
        fileAdded.Should().BeTrue($"file '{filePath}' should have been created");
        return fileContent;
    }

    private class StepDefinitionJumpListData
    {
        public string StepDefinition { get; set; }
        public string StepType { get; set; }
    }

    private class StepDefinitionSnippetData
    {
        public string Type { get; set; }
        public string Regex { get; set; }
        public string Expression { get; set; }
    }
}
