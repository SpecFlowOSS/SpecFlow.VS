using System;
using SpecFlow.VisualStudio.Editor.Services.Formatting;

namespace SpecFlow.VisualStudio.Tests.Editor.Commands;
// See Gherkin formatting tests also in GherkinDocumentFormatterTests

public class AutoFormatDocumentCommandTests
{
    private readonly StubIdeScope _ideScope;

    public AutoFormatDocumentCommandTests(ITestOutputHelper testOutputHelper)
    {
        _ideScope = new StubIdeScope(testOutputHelper);
    }

    private AutoFormatDocumentCommand CreateSUT() => new(_ideScope,
        new StubBufferTagAggregatorFactoryService(_ideScope), _ideScope.MonitoringService,
        new GherkinDocumentFormatter());

    private StubWpfTextView CreateTextView(TestText inputText, string newLine = null) =>
        StubWpfTextView.CreateTextView(_ideScope, inputText, newLine);

    [Fact]
    public void Should_format_simple_document()
    {
        var command = CreateSUT();
        var inputText = new TestText(
            @"Feature: foo",
            @"Scenario: bar",
            @"Given baz",
            @"");

        var textView = CreateTextView(inputText);

        command.PreExec(textView, AutoFormatDocumentCommand.FormatDocumentKey);

        var expectedText = new TestText(
            @"Feature: foo",
            @"Scenario: bar",
            @"    Given baz",
            @"");
        Assert.Equal(expectedText.ToString(), textView.TextSnapshot.GetText());
    }

    [Fact]
    public void Should_move_caret_to_the_end_of_the_original_caret_line()
    {
        var command = CreateSUT();
        var inputText = new TestText(
            @"Feature: foo",
            @"Scenario: bar",
            @"Given baz",
            @"");

        var textView = CreateTextView(inputText);
        inputText.MoveCaretTo(textView, 2, 6);

        command.PreExec(textView, AutoFormatDocumentCommand.FormatDocumentKey);

        var expectedText = new TestText(
            @"Feature: foo",
            @"Scenario: bar",
            @"    Given baz",
            @"");
        Assert.Equal(expectedText.ToString(), textView.TextSnapshot.GetText());
        // cursor moved to the end of the line
        expectedText.AssertCaretAt(textView, 2, expectedText.Lines[2].Length);
    }
}
