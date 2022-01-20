﻿using Microsoft.VisualStudio.Text.Tagging;

#nullable enable
namespace SpecFlow.VisualStudio.Tests.Editor.Commands;

public abstract class CommandTestBase<T> : EditorTestBase where T : DeveroomEditorCommandBase
{
    private readonly Func<InMemoryStubProjectScope, IDeveroomTaggerProvider, T> _commandFactory;
    protected readonly string WarningHeader;

    protected CommandTestBase(
        ITestOutputHelper testOutputHelper,
        Func<InMemoryStubProjectScope, IDeveroomTaggerProvider, T> commandFactory,
        string warningHeader) : base(testOutputHelper)
    {
        _commandFactory = commandFactory;
        WarningHeader = warningHeader;
    }

    protected Task<(IWpfTextView textView, T command)> ArrangeSut(
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

    protected async Task<(IWpfTextView textView, T command)> ArrangeSut(
        TestStepDefinition[] stepDefinitions,
        TestFeatureFile[] featureFiles)
    {
        var textView = await ArrangeTextView(stepDefinitions, featureFiles);
        var ideScope = ProjectScope.IdeScope;
        var taggerProvider = CreateTaggerProvider(ideScope);
        var command = _commandFactory(ProjectScope, taggerProvider);
        return (textView, command);
    }

    protected Task InvokeAndWaitAnalyticsEvent(T command, IWpfTextView textView)
    {
        Invoke(command, textView);
        return WaitForCommandToComplete(command);
    }

    protected static bool Invoke(T command, IWpfTextView textView) =>
        command.PreExec(textView, command.Targets.First());

    protected Task WaitForCommandToComplete(T command)
    {
        CancellationTokenSource cts = Debugger.IsAttached
            ? new CancellationTokenSource(TimeSpan.FromMinutes(1))
            : new CancellationTokenSource(TimeSpan.FromSeconds(10));
        return command.Finished.WaitAsync(cts.Token);
    }

    public ImmutableArray<string> WarningMessages()
    {
        var stubLogger = GetStubLogger();
        return stubLogger.Warnings().WithoutHeader(WarningHeader).Messages;
    }
}
