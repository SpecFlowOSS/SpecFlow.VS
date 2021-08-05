using System;
using SpecFlow.VisualStudio.Editor.Commands;
using SpecFlow.VisualStudio.VsxStubs;
using SpecFlow.VisualStudio.VsxStubs.ProjectSystem;
using Xunit;
using Xunit.Abstractions;

namespace SpecFlow.VisualStudio.Tests.Editor.Commands
{
    public class AutoFormatDocumentCommandTests
    {
        private readonly StubIdeScope _ideScope;

        public AutoFormatDocumentCommandTests(ITestOutputHelper testOutputHelper)
        {
            _ideScope = new StubIdeScope(testOutputHelper);
        }

        private StubWpfTextView CreateTextView(TestText inputText, string newLine = null)
        {
            return StubWpfTextView.CreateTextView(_ideScope, inputText, newLine);
        }

        [Fact]
        public void Should_not_remove_closing_delimiter_of_an_empty_docstring()
        {
            var command = CreateSUT();
            var inputText = new TestText(
                @"Feature: foo",
                @"Scenario: bar",
                @"    Given table",  
                @"    ```",
                @"    ```",
                @"");

            var textView = CreateTextView(inputText);
            
            command.PreExec(textView, AutoFormatDocumentCommand.FormatDocumentKey);

            var expectedText = new TestText(
                @"Feature: foo",
                @"Scenario: bar",
                @"    Given table",
                @"        ```",
                @"        ```",
                @"");
            Assert.Equal(expectedText.ToString(), textView.TextSnapshot.GetText());
        }

        [Fact]
        public void Should_not_remove_empty_line_from_a_single_empty_line_docstring()
        {
            var command = CreateSUT();
            var inputText = new TestText(
                @"Feature: foo",
                @"Scenario: bar",
                @"    Given table",  
                @"    ```",
                @"    ",
                @"    ```",
                @"");

            var textView = CreateTextView(inputText);
            
            command.PreExec(textView, AutoFormatDocumentCommand.FormatDocumentKey);

            var expectedText = new TestText(
                @"Feature: foo",
                @"Scenario: bar",
                @"    Given table",
                @"        ```",
                @"        ",
                @"        ```",
                @"");
            Assert.Equal(expectedText.ToString(), textView.TextSnapshot.GetText());
        }

        [Fact]
        public void Should_not_remove_whitespace_from_a_single_whitespace_line_docstring()
        {
            var command = CreateSUT();
            var inputText = new TestText(
                @"Feature: foo",
                @"Scenario: bar",
                @"    Given table",  
                @"    ```",
                @"     ",
                @"    ```",
                @"");

            var textView = CreateTextView(inputText);
            
            command.PreExec(textView, AutoFormatDocumentCommand.FormatDocumentKey);

            var expectedText = new TestText(
                @"Feature: foo",
                @"Scenario: bar",
                @"    Given table",
                @"        ```",
                @"         ",
                @"        ```",
                @"");
            Assert.Equal(expectedText.ToString(), textView.TextSnapshot.GetText());
        }

        private AutoFormatDocumentCommand CreateSUT()
        {
            return new AutoFormatDocumentCommand(_ideScope, new StubBufferTagAggregatorFactoryService(_ideScope), _ideScope.MonitoringService);
        }
    }
}
