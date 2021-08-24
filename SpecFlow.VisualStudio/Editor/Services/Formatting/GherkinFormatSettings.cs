using System;
using System.Linq;

namespace SpecFlow.VisualStudio.Editor.Services.Formatting
{
    public class GherkinFormatSettings
    {
        public string Indent { get; set; } = "    ";

        public int FeatureChildrenIndentLevel { get; set; } = 0;

        public int RuleChildrenIndentLevelWithinRule { get; set; } = 0;

        public int StepIndentLevelWithinStepContainer { get; set; } = 1;

        public int AndStepIndentLevelWithinSteps { get; set; } = 0;

        public int DataTableIndentLevelWithinStep { get; set; } = 1;
        public int DocStringIndentLevelWithinStep { get; set; } = 1;

        public int ExamplesBlockIndentLevelWithinScenarioOutline { get; set; } = 0;

        public int ExamplesTableIndentLevelWithinExamplesBlock { get; set; } = 1;

        public string TableCellPadding { get; set; } = " ";
    }
}
