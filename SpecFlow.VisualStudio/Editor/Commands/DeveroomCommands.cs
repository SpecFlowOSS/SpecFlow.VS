using System;

namespace SpecFlow.VisualStudio.Editor.Commands
{
    public static class DeveroomCommands
    {
        public static readonly Guid DefaultCommandSet = new Guid("a47e5527-9920-4715-b528-a569a7275ab3");

        public const int DefineStepsCommandId = 0x0100;
        public const int FindStepDefinitionUsagesCommandId = 0x0101;
        public const int RegenerateAllFeatureFileCodeBehindCommandId = 0x0102;
        public const int RenameStepCommandId = 0x0103;
    }
}