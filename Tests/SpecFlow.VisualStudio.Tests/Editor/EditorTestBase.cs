namespace SpecFlow.VisualStudio.Tests.Editor;

public abstract class EditorTestBase
{
    protected readonly ITestOutputHelper TestOutputHelper;
    protected readonly InMemoryStubProjectScope ProjectScope;

    protected EditorTestBase(ITestOutputHelper testOutputHelper)
    {
        TestOutputHelper = testOutputHelper;
        StubIdeScope ideScope = new StubIdeScope(testOutputHelper);
        ProjectScope = new InMemoryStubProjectScope(ideScope);
    }

    protected StubWpfTextView ArrangeTextView(
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
        ProjectScope.IdeScope.GetTextBuffer(new SourceLocation(featureFile.FileName, 1, 1),
            out var featureFileTextBuffer);
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
            @"Feature: Feature1
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
            ArrangeStepDefinition(@"""I press add"""),
            ArrangeStepDefinition(@"""I press add""", "Given"),
            ArrangeStepDefinition(@"""I select add""")
        };
        return stepDefinitions;
    }

    protected static TestStepDefinition ArrangeStepDefinition(string textExpression, string keyWord = "When",
        string attributeName = null)
    {
        var token = textExpression == null
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

    protected void ModifyFeatureFileInEditor(TestFeatureFile featureFile, Span span, string replacementText)
    {
        var sl = new SourceLocation(featureFile.FileName, 1, 1);
        ProjectScope.IdeScope.OpenIfNotOpened(featureFile.FileName);
        ProjectScope.IdeScope.GetTextBuffer(sl, out var textBuffer);

        using var textEdit = textBuffer.CreateEdit();
        textEdit.Replace(span, replacementText);

        textEdit.Apply();
    }

    protected StubLogger GetStubLogger()
    {
        return GetStubLogger(ProjectScope.IdeScope as StubIdeScope);
    }

    protected static StubLogger GetStubLogger(StubIdeScope ideScope)
    {
        var stubLogger = (ideScope.Logger as DeveroomCompositeLogger).Single(logger =>
            logger.GetType() == typeof(StubLogger)) as StubLogger;
        return stubLogger;
    }

    protected void ThereWereNoWarnings()
    {
        var stubLogger = GetStubLogger();
        stubLogger.Messages.Should().NotContain(msg => msg.Message.Contains("ShowProblem:"));
    }

}