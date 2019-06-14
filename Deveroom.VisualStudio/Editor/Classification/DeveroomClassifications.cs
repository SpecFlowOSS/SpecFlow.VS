using System.ComponentModel.Composition;
using System.Windows.Media;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace Deveroom.VisualStudio.Editor.Classification
{
    internal static class DeveroomClassifications
    {
        public const string Keyword = "deveroom.keyword";
        public const string Tag = "deveroom.tag";
        public const string Description = "deveroom.description";
        public const string Comment = "deveroom.comment";
        public const string DocString = "deveroom.doc_string";
        public const string DataTable = "deveroom.data_table";
        public const string DataTableHeader = "deveroom.data_table_header";

        public const string UndefinedStep = "deveroom.undefined_step";
        public const string StepPatameter = "deveroom.step_parameter";
        public const string ScenarioOutlinePlaceholder = "deveroom.scenario_outline_placeholder";

        // This disables "The field is never used" compiler's warning. Justification: the field is used by MEF.
#pragma warning disable 169

        [Export]
        [Name("deveroom")]
        [BaseDefinition("text")]
        private static ContentTypeDefinition _typeDefinition;
        
        [Export]
        [FileExtension(".feature")]
        [ContentType("deveroom")]
        private static FileExtensionToContentTypeDefinition _fileExtensionToContentTypeDefinition;


        [Export]
        [Name(Keyword)]
        [BaseDefinition("keyword")]
        private static ClassificationTypeDefinition _keywordClassificationTypeDefinition;

        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = Keyword)]
        [Name(Keyword)]
        [UserVisible(true)]
        [Order(Before = Priority.Default)]
        internal sealed class GherkinKeywordClassificationFormat : ClassificationFormatDefinition
        {
            public GherkinKeywordClassificationFormat()
            {
                this.DisplayName = "Deveroom Keyword";
            }
        }


        [Export]
        [Name(Tag)]
        [BaseDefinition("type")]
        private static ClassificationTypeDefinition _tagClassificationTypeDefinition;

        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = Tag)]
        [Name(Tag)]
        [UserVisible(true)]
        [Order(Before = Priority.Default)]
        internal sealed class GherkinTagClassificationFormat : ClassificationFormatDefinition
        {
            public GherkinTagClassificationFormat()
            {
                this.DisplayName = "Deveroom Tag";
            }
        }


        [Export]
        [Name(Description)]
        [BaseDefinition("excluded code")]
        private static ClassificationTypeDefinition _descriptionClassificationTypeDefinition;

        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = Description)]
        [Name(Description)]
        [UserVisible(true)]
        [Order(Before = Priority.Default)]
        internal sealed class GherkinDescriptionClassificationFormat : ClassificationFormatDefinition
        {
            public GherkinDescriptionClassificationFormat()
            {
                this.DisplayName = "Deveroom Description";
                this.IsItalic = true;
            }
        }


        [Export]
        [Name(DocString)]
        [BaseDefinition("string")]
        private static ClassificationTypeDefinition _docStringClassificationTypeDefinition;

        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = DocString)]
        [Name(DocString)]
        [UserVisible(true)]
        [Order(Before = Priority.Default)]
        internal sealed class GherkinDocStringClassificationFormat : ClassificationFormatDefinition
        {
            public GherkinDocStringClassificationFormat()
            {
                this.DisplayName = "Deveroom Doc String";
            }
        }


        [Export]
        [Name(DataTable)]
        [BaseDefinition("string")]
        private static ClassificationTypeDefinition _dataTableClassificationTypeDefinition;

        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = DataTable)]
        [Name(DataTable)]
        [UserVisible(true)]
        [Order(Before = Priority.Default)]
        internal sealed class GherkinDataTableClassificationFormat : ClassificationFormatDefinition
        {
            public GherkinDataTableClassificationFormat()
            {
                this.DisplayName = "Deveroom Data Table";
            }
        }


        [Export]
        [Name(DataTableHeader)]
        [BaseDefinition(DataTable)]
        private static ClassificationTypeDefinition _dataTableHeaderClassificationTypeDefinition;

        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = DataTableHeader)]
        [Name(DataTableHeader)]
        [UserVisible(true)]
        [Order(Before = Priority.Default)]
        internal sealed class GherkinDataTableHeaderClassificationFormat : ClassificationFormatDefinition
        {
            public GherkinDataTableHeaderClassificationFormat()
            {
                this.DisplayName = "Deveroom Data Table Header";
                this.IsItalic = true;
            }
        }


        [Export]
        [Name(Comment)]
        [BaseDefinition("comment")]
        private static ClassificationTypeDefinition _commentClassificationTypeDefinition;

        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = Comment)]
        [Name(Comment)]
        [UserVisible(true)]
        [Order(Before = Priority.Default)]
        internal sealed class GherkinCommentClassificationFormat : ClassificationFormatDefinition
        {
            public GherkinCommentClassificationFormat()
            {
                this.DisplayName = "Deveroom Comment";
            }
        }


        [Export]
        [Name(UndefinedStep)]
        private static ClassificationTypeDefinition _undefinedStepClassificationTypeDefinition;

        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = UndefinedStep)]
        [Name(UndefinedStep)]
        [UserVisible(true)]
        [Order(Before = Priority.Default)]
        internal sealed class GherkinUndefinedStepClassificationFormat : ClassificationFormatDefinition
        {
            public GherkinUndefinedStepClassificationFormat()
            {
                this.DisplayName = "Deveroom Undefined Step";
                this.ForegroundColor = Colors.Coral;
            }
        }


        [Export]
        [Name(StepPatameter)]
        [BaseDefinition("string")]
        private static ClassificationTypeDefinition _stepParameterClassificationTypeDefinition;

        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = StepPatameter)]
        [Name(StepPatameter)]
        [UserVisible(true)]
        [Order(Before = Priority.Default)]
        internal sealed class GherkinStepParameterClassificationFormat : ClassificationFormatDefinition
        {
            public GherkinStepParameterClassificationFormat()
            {
                this.DisplayName = "Deveroom Step Parameter";
            }
        }


        [Export]
        [Name(ScenarioOutlinePlaceholder)]
        [BaseDefinition("number")]
        private static ClassificationTypeDefinition _scenarioOutlinePlaceholderClassificationTypeDefinition;

        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = ScenarioOutlinePlaceholder)]
        [Name(ScenarioOutlinePlaceholder)]
        [UserVisible(true)]
        [Order(Before = Priority.Default)]
        internal sealed class GherkinScenarioOutlinePlaceholderClassificationFormat : ClassificationFormatDefinition
        {
            public GherkinScenarioOutlinePlaceholderClassificationFormat()
            {
                this.DisplayName = "Deveroom Scenario Outline Placeholder";
                this.IsItalic = true;
            }
        }



#if DEBUG
        public const string DebugMarker = "deveroom.debug_marker";

        [Export]
        [Name(DebugMarker)]
        private static ClassificationTypeDefinition _debugEditorClassificationTypeDefinition;

        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = DebugMarker)]
        [Name(DebugMarker)]
        [UserVisible(true)]
        [Order(Before = Priority.Default)]
        internal sealed class DebugEditorClassificationFormat : ClassificationFormatDefinition
        {
            public DebugEditorClassificationFormat()
            {
                this.DisplayName = "Deveroom Debugging Editor";
                this.BackgroundColor = Colors.Yellow;
            }
        }
#endif
#pragma warning restore 169
    }
}
