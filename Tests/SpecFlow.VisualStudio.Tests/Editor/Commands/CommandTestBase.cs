
namespace SpecFlow.VisualStudio.Tests.Editor.Commands;

public abstract class CommandTestBase<T> : EditorTestBase where T : DeveroomEditorCommandBase
{
    private readonly Func<IProjectScope, T> _commandFactory;
    private readonly string _completedEventSignal;
    private readonly string _warningHeader;

    protected CommandTestBase(
        ITestOutputHelper testOutputHelper,
        Func<IProjectScope, T> commandFactory,
        string completedEventSignal,
        string warningHeader) : base(testOutputHelper)
    {
        _commandFactory = commandFactory;
        _completedEventSignal = completedEventSignal;
        _warningHeader = warningHeader;
    }

    protected (StubWpfTextView textView, T command) ArrangeSut(
        TestStepDefinition stepDefinition, TestFeatureFile featureFile)
    {
        var stepDefinitions = stepDefinition.IsVoid
            ? Array.Empty<TestStepDefinition>()
            : new[] {stepDefinition};

        var featureFiles = featureFile.IsVoid
            ? Array.Empty<TestFeatureFile>()
            : new[] {featureFile};

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

    protected Task<IAnalyticsEvent> Invoke(T command, StubWpfTextView textView)
    {
        command.PreExec(textView, command.Targets.First());
        return WaitForCommandToComplete();
    }

    protected Task<IAnalyticsEvent> WaitForCommandToComplete()
    {
        return ProjectScope.StubIdeScope.AnalyticsTransmitter
            .WaitForEventAsync(_completedEventSignal);
    }

    public string WithoutWarningHeader(string message)
    {
        return message.Replace(_warningHeader, "");
    }
}
