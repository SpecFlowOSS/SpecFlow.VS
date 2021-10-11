using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.Text;
using SpecFlow.VisualStudio.Discovery;
using SpecFlow.VisualStudio.Editor.Commands;
using SpecFlow.VisualStudio.Editor.Services;
using SpecFlow.VisualStudio.ProjectSystem.Actions;
using SpecFlow.VisualStudio.VsxStubs;
using SpecFlow.VisualStudio.VsxStubs.ProjectSystem;
using SpecFlow.VisualStudio.VsxStubs.StepDefinitions;
using Xunit;
using Xunit.Abstractions;

namespace SpecFlow.VisualStudio.Tests.Editor.Commands
{
    public class FindStepDefinitionUsagesCommandTests : CommandTestBase<FindStepDefinitionUsagesCommand>
    {
        public FindStepDefinitionUsagesCommandTests(ITestOutputHelper testOutputHelper) : 
            base(testOutputHelper, 
                ps => new FindStepDefinitionUsagesCommand(ps.IdeScope, null, ps.IdeScope.MonitoringService),
                "FindStepDefinitionUsages command executed")
        {
        }

        [Fact]
        public async Task Find_usages_in_a_modified_feature_file_too()
        {
            var stepDefinition = ArrangeStepDefinition(@"""I choose add""");
            TestFeatureFile featureFile = ArrangeOneFeatureFile();
            var (textView, command) = ArrangeSut(stepDefinition, featureFile);

            ModifyFeatureFileInEditor(featureFile, new Span(50, 16), "When I choose add");
            Dump(featureFile, "After modification");
            await Invoke(command, textView);

            (ProjectScope.IdeScope.Actions as StubIdeActions).LastShowContextMenuItems.Should()
                .Contain(mi => mi.Label == "calculator.feature(3,8): When I choose add");
        }

        [Fact]
        public async Task Could_not_find_any_usage()
        {
            var stepDefinition = ArrangeStepDefinition(@"""I choose add""");
            var featureFile = ArrangeOneFeatureFile();
            var (textView, command) = ArrangeSut(stepDefinition, featureFile);

            await Invoke(command, textView);

            (ProjectScope.IdeScope.Actions as StubIdeActions).LastShowContextMenuItems.Should()
                .Contain(mi => mi.Label == "Could not find any usage");
        }

        [Theory]
        [InlineData("01", @"""I press add""")]
        [InlineData("02", @"""I press (.*)""")]
        [InlineData("03", @"""I (.*) add""")]
        public async Task Find_usages(string _, string expression)
        {
            var stepDefinition = ArrangeStepDefinition(expression);
            var featureFile = ArrangeOneFeatureFile();
            var (textView, command) = ArrangeSut(stepDefinition, featureFile);

            await Invoke(command, textView);

            (ProjectScope.IdeScope.Actions as StubIdeActions).LastShowContextMenuItems.Should()
                .Contain(mi => mi.Label == "calculator.feature(3,8): When I press add");
        }
    }
}
